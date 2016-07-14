// <copyright file="MulticastSession.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using IO;
    using PCLStorage;
    using Properties;
    using Session;

    /// <summary>
    /// Represent a multicast session.
    /// </summary>
    /// <seealso cref="ServerBase" />
    public class MulticastSession : ServerBase, IEquatable<MulticastSession>
    {
        private ILog log = LogManager.GetLogger<MulticastSession>();
        private BitVector written;
        private ChunkReader reader;
        private FileSet fileSet;
        private UdpWriter writer;
        private SeqNum seqNum;
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
                return this.fileSet.FileHeaders.Select((h) => h.Name)
                                               .ToList();
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
                if (this.reader != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in this.reader.Chunks)
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
                if (this.reader != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in this.reader.Chunks)
                    {
                        if (!this.reader.Read[chunk.Block.SegmentId])
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
        /// Gets the current sequence number in the download.
        /// </summary>
        /// <value>
        /// The sequence number.
        /// </value>
        public long SequenceNumber
        {
            get
            {
                SeqNum seq = this.seqNum;
                if (seq != null)
                {
                    return seq.Seq;
                }

                return 0;
            }
        }

        internal MulticastServer Server
        {
            get;
            private set;
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

        internal Task StartSession(CancellationToken token)
        {
            this.log.Debug("Starting session: " + this);
            throw new NotImplementedException();
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

        internal Task TransmitWave(CancellationToken token)
        {
            throw new NotImplementedException();
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
    }
}
