// <copyright file="MulticastSession.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.IO;
    using Core.Session;
    using Cryptography;
    using IO;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent a multicast session.
    /// </summary>
    /// <seealso cref="ServerBase" />
    /// <seealso cref="ITransferReporting" />
    /// <seealso cref="ISequenceReporting" />
    public class MulticastSession : ServerBase, IEquatable<MulticastSession>, ITransferReporting, ISequenceReporting
    {
        private ILog log = LogManager.GetLogger<MulticastSession>();
        private BitVector written;
        private ChunkReader reader;
        private FileSet fileSet;
        private UdpWriter writer;
        private BoxedLong seqNum;
        private BoxedLong waveNum;
        private ThroughputCalculator throughputCalculator = new ThroughputCalculator(Constants.MaxIntervals);
        private BoxedLong bytesPerSecond;
        private bool disposed;

        internal MulticastSession(MulticastServer server, string path, int sessionId)
        {
            Contract.Requires(server != null && !string.IsNullOrEmpty(path));
            Contract.Requires(sessionId >= 0);
            this.Server = server;
            this.SessionId = sessionId;
            this.Path = path;
        }

        /// <summary>
        /// Gets the written bits.
        /// </summary>
        /// <value>
        /// The written bits.
        /// </value>
        public BitVector Written
        {
            get
            {
                return this.written;
            }
        }

        /// <summary>
        /// Gets the session identifier.
        /// </summary>
        /// <value>
        /// The session identifier.
        /// </value>
        public int SessionId
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets file the path.
        /// </summary>
        /// <value>
        /// The file path.
        /// </value>
        public string Path
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the files being transmitted by the session, relative to the root folder.
        /// </summary>
        /// <value>
        /// The files.
        /// </value>
        public ICollection<string> Files
        {
            get
            {
                if (this.fileSet != null)
                {
                    return this.fileSet.FileHeaders.Select((h) => h.Name)
                                                   .ToList();
                }

                return new string[0];
            }
        }

        /// <summary>
        /// Gets the total bytes in the payload.
        /// </summary>
        /// <value>
        /// The total bytes in the payload.
        /// </value>
        public long TotalBytes
        {
            get
            {
                ChunkReader reader = this.reader;
                if (reader != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in reader.Chunks)
                    {
                        bytes += chunk.Block.Length;
                    }

                    return bytes;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the bytes remaining in the payload.
        /// </summary>
        /// <value>
        /// The bytes remaining.
        /// </value>
        public long BytesRemaining
        {
            get
            {
                ChunkReader reader = this.reader;
                if (reader != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in reader.Chunks)
                    {
                        if (!reader.Read[chunk.Block.SegmentId])
                        {
                            bytes += chunk.Block.Length;
                        }
                    }

                    return bytes;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the bytes per second.
        /// </summary>
        /// <value>
        /// The bytes per second.
        /// </value>
        public long BytesPerSecond
        {
            get
            {
                BoxedLong bps = this.bytesPerSecond;
                if (bps != null)
                {
                    return bps.Value;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the current sequence number in the download.
        /// </summary>
        /// <value>
        /// The sequence number.
        /// </value>
        public long SequenceNumber
        {
            get
            {
                BoxedLong seq = this.seqNum;
                if (seq != null)
                {
                    return seq.Value;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the wave number for the session.
        /// </summary>
        /// <value>
        /// The wave number.
        /// </value>
        public long WaveNumber
        {
            get
            {
                BoxedLong wave = this.waveNum;
                if (wave != null)
                {
                    return wave.Value;
                }

                return 0;
            }
        }

        internal MulticastServer Server
        {
            get;
            private set;
        }

        internal string MulticastAddress
        {
            get
            {
                if (this.writer != null)
                {
                    return this.writer.MulticastAddress;
                }

                return string.Empty;
            }
        }

        internal int MulticastPort
        {
            get
            {
                if (this.writer != null)
                {
                    return this.writer.MulticastPort;
                }

                return 0;
            }
        }

        internal ICollection<FileHeader> FileHeaders
        {
            get
            {
                if (this.fileSet != null)
                {
                    return this.fileSet.FileHeaders;
                }

                return new FileHeader[0];
            }
        }

        internal long CountChunks
        {
            get
            {
                if (this.fileSet != null)
                {
                    return this.fileSet.EnumerateChunks().LongCount();
                }

                return 0;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                return base.Equals(obj as MulticastSession);
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(MulticastSession other)
        {
            if (other != null && this != other)
            {
                return this.Files.SequenceEqual(other.Files);
            }

            return this != other;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            int ret = 0;
            foreach (string file in this.Files)
            {
                ret += file.GetHashCode();
            }

            return ret;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Session:" + this.SessionId + "'" + this.Path + "'";
        }

        internal async Task StartSession(UdpWriter writer, CancellationToken token)
        {
            Contract.Requires(writer != null);
            this.writer = writer;
            this.log.Debug("Starting session: " + this);
            IFolder rootFolder = this.Server.Settings.RootFolder;
            ExistenceCheckResult pathExists = await rootFolder.CheckExistsAsync(this.Path);
            List<string> filePaths = new List<string>();
            if (pathExists == ExistenceCheckResult.FileExists)
            {
                this.log.Debug(this.Path + " is a file.");
                filePaths.Add(this.Path);
            }
            else if (pathExists == ExistenceCheckResult.FolderExists)
            {
                this.log.Debug(this.Path + " is a folder.");
                rootFolder = await rootFolder.CreateFolderAsync(this.Path, CreationCollisionOption.OpenIfExists);
                filePaths.AddRange(await rootFolder.GetFilesFromPath(true, (f) => true));
            }
            else
            {
                this.log.Error(this.Path + " not found.");
                throw new FileNotFoundException();
            }

            this.waveNum = new BoxedLong(1);
            this.fileSet = new FileSet(rootFolder, filePaths);
            await this.fileSet.InitRead(this.writer.BlockSize);
            this.written = new BitVector(this.fileSet.NumSegments);
        }

        internal void CalculatePayload(ICollection<MulticastConnection> connections)
        {
            List<BitVector> sessionVectors = new List<BitVector>(connections.Count);
            foreach (MulticastConnection conn in connections)
            {
                sessionVectors.Add(conn.Written);
            }

            BitVector newWritten = BitVector.IntersectOf(sessionVectors);
            newWritten.RawBits.CopyTo(this.written.RawBits, 0);
        }

        internal async Task TransmitWave(CancellationToken token)
        {
            long seq = 0;
            this.seqNum = new BoxedLong(seq);
            this.waveNum = new BoxedLong(this.WaveNumber + 1);
            this.reader = new ChunkReader(this.fileSet.EnumerateChunks(), this.written);
            this.log.Debug("Beginning multicast transmit wave " + this.WaveNumber);
            this.log.Debug("Bytes remaining: " + this.BytesRemaining);
            this.throughputCalculator.Start(this.BytesRemaining, DateTime.Now);

            using (CancellationTokenSource completeCts = new CancellationTokenSource())
            using (CancellationTokenRegistration waveCompletedCanceler = token.Register(() => completeCts.Cancel()))
            {
                CancellationToken completeToken = completeCts.Token;
                int burstLength = this.Server.ServerSettings.MulticastBurstLength;
                List<FileSegment> pendingSegments = new List<FileSegment>(burstLength);
                Task writeTask = null;
                Task throughputTask = null;
                for (; seq < this.written.LongCount; seq += burstLength)
                {
                    List<FileSegment> sent = new List<FileSegment>(await this.reader.ReadSegments(burstLength));
                    if (throughputTask == null || throughputTask.IsCompleted || throughputTask.IsCanceled || throughputTask.IsFaulted)
                    {
                        await CompleteThroughputTask(throughputTask);
                        throughputTask = this.UpdateStatus(seq, completeToken);
                    }

                    if (writeTask != null)
                    {
                        await writeTask;
                    }

                    writeTask = Task.WhenAll(this.writer.SendMulticast(sent, token), Task.Delay(this.Server.BurstDelayMs));
                }

                if (writeTask != null)
                {
                    await writeTask;
                }

                completeCts.Cancel();
                if (throughputTask != null)
                {
                    await CompleteThroughputTask(throughputTask);
                }
            }

            this.log.Debug("Wave complete");
        }

        internal async Task UpdateStatus(long seq, CancellationToken token)
        {
            Task delayTask = Task.Delay(Constants.PacketUpdateInterval, token);
            long bytesRemaining = this.BytesRemaining;
            for (long i = 0; i < seq; ++i)
            {
                if (!this.reader.Read[seq])
                {
                    bytesRemaining -= this.reader.Chunks[seq].Block.Length;
                    Contract.Assert(bytesRemaining >= 0);
                }
            }

            long average = this.throughputCalculator.UpdateThroughput(bytesRemaining, DateTime.Now);
            this.seqNum = new BoxedLong(seq);
            this.bytesPerSecond = new BoxedLong(average);
            await delayTask;
        }

        internal async Task Close()
        {
            this.log.Debug("Closing session: " + this);
            await this.writer.Close();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.fileSet != null)
                    {
                        this.fileSet.Dispose();
                    }

                    if (this.writer != null)
                    {
                        this.writer.Dispose();
                    }
                }
            }
        }

        private static async Task CompleteThroughputTask(Task throughputTask)
        {
            try
            {
                await throughputTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
