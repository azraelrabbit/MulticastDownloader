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
        private object readerLock = new object();
        private ChunkReader reader;
        private FileSet fileSet;
        private UdpWriter writer;
        private object seqLock = new object();
        private long seqNum;
        private long waveNum = 0;
        private object updateLock = new object();
        private long bytesSent = 0;
        private long waveSize = 0;
        private ThroughputCalculator throughputCalculator = new ThroughputCalculator(Constants.MaxIntervals);
        private long bytesPerSecond;
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
                lock (this.readerLock)
                {
                    return new BitVector(this.reader.Read.LongCount, this.reader.Read.RawBits);
                }
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
                lock (this.readerLock)
                {
                    return this.reader.TotalBytes;
                }
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
                lock (this.updateLock)
                {
                    return this.waveSize;
                }
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
                lock (this.updateLock)
                {
                    return this.bytesPerSecond;
                }
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
                lock (this.seqLock)
                {
                    return this.seqNum;
                }
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
                lock (this.seqLock)
                {
                    return this.waveNum;
                }
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

        internal long BytesSent
        {
            get
            {
                lock (this.updateLock)
                {
                    return this.bytesSent;
                }
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

            return this == other;
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
            this.log.Debug(this + ": Starting session");
            IFolder rootFolder = this.Server.Settings.RootFolder;
            ExistenceCheckResult pathExists = await rootFolder.CheckExistsAsync(this.Path);
            List<string> filePaths = new List<string>();
            if (pathExists == ExistenceCheckResult.FileExists)
            {
                this.log.Debug(this + ": " + this.Path + " is a file.");
                filePaths.Add(this.Path);
            }
            else if (pathExists == ExistenceCheckResult.FolderExists)
            {
                this.log.Debug(this + ": " + this.Path + " is a folder.");
                rootFolder = await rootFolder.CreateFolderAsync(this.Path, CreationCollisionOption.OpenIfExists);
                filePaths.AddRange(await rootFolder.GetFilesFromPath(true, (f) => true));
            }
            else
            {
                this.log.Error(this + ": " + this.Path + " not found.");
                throw new FileNotFoundException();
            }

            this.fileSet = new FileSet(rootFolder, filePaths);
            await this.fileSet.InitRead(this.writer.BlockSize);
            this.reader = new ChunkReader(this.fileSet.EnumerateChunks(), new BitVector(this.fileSet.NumSegments));
        }

        internal void BeginBurst()
        {
            lock (this.seqLock)
            {
                this.seqNum = 0;
                ++this.waveNum;
            }

            lock (this.updateLock)
            {
                this.waveSize = this.reader.BytesRemaining;
                this.bytesSent = 0;
                this.throughputCalculator.Start(this.waveSize, DateTime.Now);
                this.log.Debug(this + ": Beginning multicast transmit wave " + this.waveNum);
                this.log.Debug(this + ": Bytes remaining: " + this.waveSize);
            }

            lock (this.readerLock)
            {
                this.reader.Reset();
            }
        }

        internal async Task<ICollection<FileSegment>> ReadBurst()
        {
            return (await this.reader.ReadSegments(this.Server.ServerSettings.MulticastBurstLength)).ToArray();
        }

        internal async Task WriteBurst(ICollection<FileSegment> segments)
        {
            await Task.WhenAll(
                this.UpdateTransferStatus(segments),
                this.writer.SendMulticast(segments),
                Task.Delay(this.Server.BurstDelayMs));
        }

        internal void BeginIntersectOf()
        {
            lock (this.readerLock)
            {
                this.reader.Read.BeginIntersectOf();
            }
        }

        internal void IntersectOf(byte[] rawBits)
        {
            lock (this.readerLock)
            {
                this.reader.Read.IntersectOf(new BitVector(this.reader.Read.LongCount, rawBits));
            }
        }

        internal void UpdateSessionStatus()
        {
            lock (this.updateLock)
            {
                this.bytesPerSecond = this.throughputCalculator.UpdateThroughput(this.waveSize - this.bytesSent, DateTime.Now);
                Contract.Assert(this.bytesPerSecond >= 0);
            }
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

        private Task UpdateTransferStatus(ICollection<FileSegment> segments)
        {
            return Task.Run(() =>
            {
                FileSegment last = null;
                lock (this.updateLock)
                {
                    foreach (FileSegment seg in segments)
                    {
                        this.bytesSent += seg.Data.Length;
                        last = seg;
                    }
                }

                if (last != null)
                {
                    lock (this.seqLock)
                    {
                        this.seqNum = last.SegmentId;
                    }
                }
            });
        }
    }
}
