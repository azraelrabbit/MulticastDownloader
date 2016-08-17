// <copyright file="MulticastConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.Cryptography;
    using Core.IO;
    using Cryptography;
    using IO;
    using Properties;
    using Session;

    /// <summary>
    /// Represent an initial multicast connection.
    /// </summary>
    /// <seealso cref="IReceptionReporting"/>
    /// <seealso cref="ServerBase" />
    public class MulticastConnection : ServerBase, IEquatable<MulticastConnection>, IReceptionReporting
    {
        private ILog log = LogManager.GetLogger<MulticastConnection>();
        private object updateLock = new object();
        private long bytesRecieved = 0;
        private double receptionRate = 0;
        private bool disposed;

        internal MulticastConnection(MulticastServer server, ServerConnection serverConn)
        {
            Contract.Requires(server != null && serverConn != null);
            this.Server = server;
            this.ServerConnection = serverConn;
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
            get;
            private set;
        }

        /// <summary>
        /// Gets the remote port.
        /// </summary>
        /// <value>
        /// The remote port.
        /// </value>
        public int RemotePort
        {
            get;
            private set;
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
                lock (this.updateLock)
                {
                    return this.receptionRate;
                }
            }
        }

        /// <summary>
        /// Gets the multicast session.
        /// </summary>
        /// <value>
        /// The multicast session this connection belongs to.
        /// </value>
        public MulticastSession Session
        {
            get;
            private set;
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
            set;
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
            return "[" + (this.Session != null ? this.Session.SessionId.ToString(CultureInfo.InvariantCulture) : string.Empty) + "]" + (this.RemoteAddress ?? string.Empty) + ":" + this.RemotePort;
        }

        internal string AcceptConnection(CancellationToken token)
        {
            this.RemoteAddress = this.ServerConnection.TcpSession.RemoteAddress;
            this.RemotePort = this.ServerConnection.TcpSession.RemotePort;

            if (this.Server.Connections.Count >= this.Server.ServerSettings.MaxConnections)
            {
                throw new InvalidOperationException(Resources.TooManyConnections);
            }

            Response connectResponse = new Response();
            connectResponse.ResponseType = ResponseId.Ok;
            this.ServerConnection.Send(connectResponse, token);

            Challenge challenge = new Challenge();
            if (this.Server.Settings.Encoder != null)
            {
                IEncoder encoder = this.Server.Settings.Encoder.CreateEncoder();
                challenge.ChallengeKey = encoder.Encode(this.Server.ChallengeKey);
            }

            this.ServerConnection.Send(challenge, token);
            if (this.Server.Parameters.UseTls)
            {
                // We use TLS to secure the connection before sending back the decoded response.
                byte[] psk = new byte[this.Server.ChallengeKey.Length];
                Array.Copy(this.Server.ChallengeKey, psk, this.Server.ChallengeKey.Length);
                this.ServerConnection.AcceptTls(psk);
            }

            ChallengeResponse challengeResponse = this.ServerConnection.Receive<ChallengeResponse>(token);
            Response authResponse = new Response();
            authResponse.ResponseType = ResponseId.Ok;
            if (challenge.ChallengeKey != null)
            {
                this.log.Debug(this + ": Authenticating challenge");
                IDecoder decoder = this.Server.EncoderFactory.CreateDecoder();
                byte[] responsePhrase = decoder.Decode(challengeResponse.ChallengeKey);
                if (!responsePhrase.SequenceEqual(Constants.ResponseId))
                {
                    authResponse.ResponseType = ResponseId.AccessDenied;
                    authResponse.Message = Resources.AuthenticationFailed;
                }
            }

            this.ServerConnection.Send(authResponse, token);
            authResponse.ThrowIfFailed();
            this.log.Info(this + ": Connection accepted.");

            SessionJoinRequest request = this.ServerConnection.Receive<SessionJoinRequest>(token);
            this.log.InfoFormat(this + ": Payload '{0}' requested with state: {1}", request.Path ?? "<null>", request.State);
            this.State = request.State;
            return request.Path;
        }

        internal void JoinSession(MulticastSession session, CancellationToken timeoutToken)
        {
            DateTime now = DateTime.Now;
            this.WhenJoined = now;
            this.WhenExpires = now + this.Server.Settings.ReadTimeout;
            this.Session = session;
            SessionJoinResponse sjr = new SessionJoinResponse();
            sjr.ResponseType = ResponseId.Ok;
            sjr.Ipv6 = this.Server.ServerSettings.Ipv6;
            sjr.MulticastAddress = session.MulticastAddress;
            sjr.MulticastPort = session.MulticastPort;
            sjr.Files = session.FileHeaders.ToArray();
            sjr.WaveNumber = session.WaveNumber;
            this.log.DebugFormat(this + ": SJR: " + sjr.MulticastAddress + ":" + sjr.MulticastPort + "; " + sjr.CountSegments + " segments, session id: " + this.Session.SessionId + ", wave: " + sjr.WaveNumber);
            this.ServerConnection.Send(sjr, timeoutToken);
        }

        internal void SessionJoinFailed(CancellationToken timeoutToken, Exception ex)
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
            this.log.Error(this + ": Join failed: " + sjr.ResponseType + " (" + sjr.Message + ")");
            this.ServerConnection.Send(sjr, timeoutToken);
        }

        internal void UpdatePacketStatus(PacketStatusUpdate psu, DateTime when)
        {
            this.WhenExpires = when + this.Server.Settings.ReadTimeout;
            lock (this.updateLock)
            {
                this.bytesRecieved = psu.BytesRecieved;
                long bytesSent = this.Session.BytesSent;
                if (bytesSent > 0)
                {
                    double rr = (double)this.bytesRecieved / (double)bytesSent;
                    if (rr < 0.0)
                    {
                        rr = 0.0;
                    }

                    if (rr > 1.0)
                    {
                        rr = 1.0;
                    }

                    this.receptionRate = rr;
                }
            }

            if (psu.LeavingSession && !this.LeavingSession)
            {
                this.LeavingSession = true;
                this.log.Debug(this + ": leaving session");
            }
        }

        internal void UpdateWaveStatus(WaveStatusUpdate wsu, DateTime when)
        {
            this.UpdatePacketStatus(wsu, when);
            this.Session.IntersectOf(wsu.FileBitVector);
            wsu.FileBitVector = null;
        }

        internal async Task Close()
        {
            this.log.InfoFormat(this + ": Closing connection");
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
