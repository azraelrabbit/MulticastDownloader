// <copyright file="MulticastConnection.cs" company="MS">
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
    using Core.Cryptography;
    using Cryptography;
    using IO;
    using Properties;
    using Session;

    /// <summary>
    /// Represent an initial multicast connection.
    /// </summary>
    /// <seealso cref="ITransferReporting" />
    /// <seealso cref="ISequenceReporting"/>
    /// <seealso cref="IReceptionReporting"/>
    /// <seealso cref="ServerBase" />
    public class MulticastConnection : ServerBase, IEquatable<MulticastConnection>, ITransferReporting, ISequenceReporting, IReceptionReporting
    {
        private ILog log = LogManager.GetLogger<MulticastConnection>();
        private BoxedLong bytesPerSecond;
        private ThroughputCalculator throughputCalculator = new ThroughputCalculator(MulticastClient.MaxIntervals);
        private BitVector written;
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
                if (this.Session != null)
                {
                    return this.Session.TotalBytes;
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
                if (this.Session != null)
                {
                    return this.Session.BytesRemaining;
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
                if (this.Session != null)
                {
                    return this.Session.SequenceNumber;
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
                if (this.Session != null)
                {
                    return this.Session.WaveNumber;
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
        /// Gets the packet reception rate for this connection.
        /// </summary>
        /// <value>
        /// The reception rate, as a coefficient in the range [0,1].
        /// </value>
        public double ReceptionRate
        {
            get
            {
                if (this.Session != null)
                {
                    long receiveRate = this.BytesPerSecond;
                    long transmitRate = this.Session.BytesPerSecond;
                    if (transmitRate > 0)
                    {
                        double ret = (double)receiveRate / (double)transmitRate;
                        Contract.Assert(ret >= 0 && ret <= 1.0);
                    }
                }

                return 0.0;
            }
        }

        internal MulticastServer Server
        {
            get;
            private set;
        }

        internal ServerConnection ServerConnection
        {
            get;
            private set;
        }

        internal MulticastSession Session
        {
            get;
            private set;
        }

        internal DateTime WhenJoined
        {
            get;
            private set;
        }

        internal DateTime WhenExpires
        {
            get;
            private set;
        }

        internal bool LeavingSession
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
            return "[" + (this.Session != null ? this.Session.SessionId.ToString() : string.Empty) + "]" + (this.RemoteAddress ?? string.Empty) + ":" + this.RemotePort;
        }

        internal async Task<string> AcceptConnection(CancellationToken token)
        {
            Challenge challenge = new Challenge();
            if (this.Server.Settings.Encoder != null)
            {
                IEncoder encoder = this.Server.Settings.Encoder.CreateEncoder();
                challenge.ChallengeKey = encoder.Encode(this.Server.ChallengeKey);
            }

            await this.ServerConnection.Send(challenge, token);
            if (this.Server.Parameters.UseTls)
            {
                // We use TLS to secure the connection before sending back the decoded response.
                this.ServerConnection.AcceptTls(this.Server.ChallengeKey);
            }

            ChallengeResponse challengeResponse = await this.ServerConnection.Receive<ChallengeResponse>(token);
            Response authResponse = new Response();
            authResponse.ResponseType = ResponseId.Ok;
            if (challenge.ChallengeKey != null)
            {
                this.log.Debug("Authenticating challenge");
                IDecoder decoder = this.Server.EncoderFactory.CreateDecoder();
                byte[] responsePhrase = decoder.Decode(challengeResponse.ChallengeKey);
                if (!responsePhrase.SequenceEqual(MulticastClient.ResponseId))
                {
                    authResponse.ResponseType = ResponseId.AccessDenied;
                    authResponse.Message = Resources.AuthenticationFailed;
                }
            }

            await this.ServerConnection.Send(authResponse, token);
            authResponse.ThrowIfFailed();
            this.log.Info("Connection from " + this + " accepted.");

            SessionJoinRequest request = await this.ServerConnection.Receive<SessionJoinRequest>(token);
            this.log.InfoFormat("Payload '{0}' requested with state: {1}", request.Path, request.State);
            this.State = request.State;
            return request.Path;
        }

        internal async Task JoinSession(MulticastSession session, CancellationToken timeoutToken)
        {
            DateTime now = DateTime.Now;
            this.throughputCalculator.Start(session.TotalBytes, now);
            this.WhenJoined = now;
            this.bytesPerSecond = new BoxedLong(0);
            this.WhenExpires = now + this.Server.Settings.ReadTimeout;
            this.Session = session;
            SessionJoinResponse sjr = new SessionJoinResponse();
            sjr.ResponseType = ResponseId.Ok;
            sjr.Ipv6 = this.Server.ServerSettings.Ipv6;
            sjr.MulticastAddress = session.MulticastAddress;
            sjr.MulticastPort = session.MulticastPort;
            sjr.Files = session.FileHeaders.ToArray();
            sjr.WaveNumber = session.WaveNumber;
            this.log.DebugFormat("SJR: " + sjr.MulticastAddress + ":" + sjr.MulticastPort + "; " + sjr.CountSegments + " segments, session id: " + this.Session.SessionId + ", wave: " + sjr.WaveNumber);
            this.written = new BitVector(session.CountChunks);
            await this.ServerConnection.Send(sjr, timeoutToken);
        }

        internal async Task SessionJoinFailed(CancellationToken timeoutToken, Exception ex)
        {
            SessionJoinResponse sjr = new SessionJoinResponse();
            if (ex is FileNotFoundException)
            {
                sjr.ResponseType = ResponseId.PathNotFound;
            }
            else if (ex is InvalidOperationException)
            {
                sjr.ResponseType = ResponseId.InvalidOperation;
            }
            else
            {
                sjr.ResponseType = ResponseId.Failed;
            }

            sjr.Message = ex.Message;
            this.log.Error("Join failed: " + sjr.ResponseType + " (" + sjr.Message + ")");
            await this.ServerConnection.Send(sjr, timeoutToken);
        }

        internal void UpdatePacketStatus(PacketStatusUpdate psu, DateTime when, CancellationToken token)
        {
            this.WhenExpires = when + this.Server.Settings.ReadTimeout;
            long average = this.throughputCalculator.UpdateThroughput(psu.BytesLeft, when);
            this.bytesPerSecond = new BoxedLong(average);
            this.log.Debug("[" + this + "] Bytes left: " + psu.BytesLeft + ", bytes per second: " + average);
            if (psu.LeavingSession && this.LeavingSession != psu.LeavingSession)
            {
                this.LeavingSession = psu.LeavingSession;
                this.log.Debug("[" + this + "] leaving session");
            }
        }

        internal void UpdateWaveStatus(WaveStatusUpdate wsu, DateTime when, CancellationToken token)
        {
            this.UpdatePacketStatus(wsu, when, token);
            long len = this.written.Count;
            this.written = new BitVector(len, wsu.FileBitVector);
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
                }
            }
        }
    }
}
