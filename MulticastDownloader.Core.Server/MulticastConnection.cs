// <copyright file="MulticastConnection.cs" company="MS">
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
    using Properties;
    using Session;

    /// <summary>
    /// Represent an initial multicast connection.
    /// </summary>
    /// <seealso cref="ServerBase" />
    public class MulticastConnection : ServerBase, IEquatable<MulticastConnection>
    {
        private ILog log = LogManager.GetLogger<MulticastConnection>();
        private BitVector written;
        private ChunkReader reader;
        private UdpWriter writer;
        private bool listening;
        private bool disposed;

        internal MulticastConnection(MulticastServer server, ServerConnection serverConn)
        {
            Contract.Requires(server != null && serverConn != null);
            this.Server = server;
            this.ServerConnection = serverConn;
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
        /// Gets the user-defined session state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public int State
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the remote address.
        /// </summary>
        /// <value>
        /// The remote address.
        /// </value>
        public string RemoteAddress
        {
            get
            {
                if (this.ServerConnection != null)
                {
                    return this.ServerConnection.TcpSession.RemoteAddress;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the remote port.
        /// </summary>
        /// <value>
        /// The remote port.
        /// </value>
        public int RemotePort
        {
            get
            {
                if (this.ServerConnection != null)
                {
                    return this.ServerConnection.TcpSession.RemotePort;
                }

                return int.MinValue;
            }
        }

        internal ServerConnection ServerConnection
        {
            get;
            private set;
        }

        internal MulticastServer Server
        {
            get;
            private set;
        }

        internal DateTime WhenExpires
        {
            get;
            set;
        }

        internal int SessionId
        {
            get;
            private set;
        }

        internal bool LeavingSession
        {
            get;
            private set;
        }

        internal bool TcpDownload
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
            if (obj is MulticastConnection)
            {
                return this.Equals(obj as MulticastConnection);
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
        public bool Equals(MulticastConnection other)
        {
            if (other != null && this != other)
            {
                return string.Compare(this.RemoteAddress, other.RemoteAddress, StringComparison.Ordinal) == 0
                    && this.RemotePort == other.RemotePort;
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
            return this.RemoteAddress.GetHashCode() + this.RemotePort.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "[" + this.SessionId + "]" + (this.RemoteAddress ?? string.Empty) + ":" + this.RemotePort;
        }

        internal Task<SessionJoinRequest> AcceptConnection(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        internal Task TransmitTcp(MulticastSession session, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        internal Response UpdatePacketStatus(PacketStatusUpdate psu, bool waveComplete, CancellationToken token)
        {
            throw new NotImplementedException();
            //mc.UpdateBytesLeft(psu.BytesLeft, psu.LeavingSession);
            //Response response = new Response();
            //if (waveComplete)
            //{
            //    response.ResponseType = ResponseId.WaveComplete;
            //}
            //else
            //{
            //    response.ResponseType = ResponseId.Ok;
            //}

            //return response;
        }

        internal WaveCompleteResponse UpdateWaveStatus(WaveStatusUpdate wsu, CancellationToken token)
        {
            throw new NotImplementedException();
            //WaveCompleteResponse response = new WaveCompleteResponse();
            //mc.UpdateWave(wsu.BytesLeft, wsu.FileBitVector, wsu.LeavingSession);
            //response.DirectDownload = mc.TcpDownload;
            //response.ResponseType = ResponseId.Ok;
            //return response;
        }

        internal async Task Close()
        {
            this.log.InfoFormat("Closing connection: " + this);
            await this.ServerConnection.Close();
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
                    if (this.ServerConnection != null)
                    {
                        this.ServerConnection.Dispose();
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
