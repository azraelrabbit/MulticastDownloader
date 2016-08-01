// <copyright file="MulticastServer.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
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
    using Org.BouncyCastle.Security;
    using PCLStorage;
    using Properties;
    using Session;

    /// <summary>
    /// Represent a multicast server.
    /// </summary>
    /// <seealso cref="ServerBase" />
    /// <seealso cref="MulticastConnection" />
    /// <seealso cref="MulticastSession" />
    /// <seealso cref="IReceptionReporting" />
    public class MulticastServer : ServerBase, ITransferReporting, IReceptionReporting
    {
        // Clients much respond to any request with the given interval
        internal static readonly TimeSpan ResponseDelay = TimeSpan.FromSeconds(60);

        private const int MinBurstDelay = 1;
        private const int StartBurstDelay = 10;
        private const int MaxBurstDelay = 999;
        private const double DecreaseThreshold = 0.98;
        private const double IncreaseThreshold = 0.90;

        private ILog log = LogManager.GetLogger<MulticastServer>();
        private object connectionLock = new object();
        private object sessionLock = new object();
        private List<MulticastConnection> connections = new List<MulticastConnection>();
        private List<MulticastSession> sessions = new List<MulticastSession>();
        private MulticastSession activeSession;
        private AutoResetEvent joinEvent = new AutoResetEvent(false);
        private ServerListener listener;
        private IUdpMulticast udpMulticast;
        private HashEncoderFactory encoderFactory;
        private BoxedDouble receptionRate = new BoxedDouble(1.0);
        private CancellationToken token = CancellationToken.None;
        private Task acceptTask;
        private bool listening;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastServer"/> class.
        /// </summary>
        /// <param name="udpMulticast">The <see cref="IUdpMulticast"/> implementation.</param>
        /// <param name="path">The server path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="serverSettings">The server settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastServer(IUdpMulticast udpMulticast, Uri path, IMulticastSettings settings, IMulticastServerSettings serverSettings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.Validate();

            if (serverSettings == null)
            {
                throw new ArgumentNullException("serverSettings");
            }

            serverSettings.Validate();
            this.Parameters = new UriParameters(path);
            this.Settings = settings;
            this.ServerSettings = serverSettings;
            if (this.Parameters.UseTls && this.Settings.Encoder == null)
            {
                throw new ArgumentException(Resources.MustSpecifyEncoder, "settings");
            }

            this.listener = new ServerListener(this.Parameters, settings, serverSettings);
            if (settings.Encoder != null)
            {
                SecureRandom sr = new SecureRandom();
                this.ChallengeKey = new byte[Constants.EncoderSize / 8];
                sr.NextBytes(this.ChallengeKey);
                this.encoderFactory = new HashEncoderFactory(this.ChallengeKey, Constants.EncoderSize);
            }

            this.udpMulticast = udpMulticast;
        }

        /// <summary>
        /// Gets the active connections.
        /// </summary>
        /// <value>
        /// The connections.
        /// </value>
        public ICollection<MulticastConnection> Connections
        {
            get
            {
                lock (this.connectionLock)
                {
                    return this.connections.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the active sessions.
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        public ICollection<MulticastSession> Sessions
        {
            get
            {
                lock (this.sessionLock)
                {
                    return this.sessions.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public UriParameters Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the multicast settings.
        /// </summary>
        /// <value>
        /// The multicast settings.
        /// </value>
        public IMulticastSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the multicast server settings.
        /// </summary>
        /// <value>
        /// The multicast server settings.
        /// </value>
        public IMulticastServerSettings ServerSettings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the packet reception rate.
        /// </summary>
        /// <value>
        /// The reception rate, as a coefficient in the range [0,1].
        /// </value>
        public double ReceptionRate
        {
            get
            {
                BoxedDouble receptionRate = this.receptionRate;
                if (receptionRate != null)
                {
                    return receptionRate.Value;
                }

                return 0;
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
                lock (this.sessionLock)
                {
                    long ret = 0;
                    foreach (MulticastSession session in this.sessions)
                    {
                        ret += session.TotalBytes;
                    }

                    return ret;
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
                lock (this.sessionLock)
                {
                    long ret = 0;
                    foreach (MulticastSession session in this.sessions)
                    {
                        ret += session.BytesRemaining;
                    }

                    return ret;
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
                MulticastSession session = this.activeSession;
                if (session != null)
                {
                    return session.BytesPerSecond;
                }

                return 0;
            }
        }

        internal byte[] ChallengeKey
        {
            get;
            private set;
        }

        internal int BurstDelayMs
        {
            get;
            private set;
        }

        internal IEncoderFactory EncoderFactory
        {
            get
            {
                return this.encoderFactory;
            }
        }

        /// <summary>
        /// Listens for requests.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>A task object.</returns>
        /// <exception cref="SessionAbortedException">Occurs if the server is aborted.</exception>
        public Task Listen(CancellationToken token)
        {
            this.token = token;
            return Task.Run(async () =>
            {
                this.log.Info("Starting multicast server");
                try
                {
                    this.acceptTask = this.AcceptAndJoinClients(token);
                    using (CancellationTokenRegistration registration = this.token.Register(this.CancelListen))
                    {
                        while (this.listening)
                        {
                            if (this.acceptTask.IsFaulted || this.acceptTask.IsCanceled || this.acceptTask.IsCompleted)
                            {
                                // We may have gotten hanked.
                                await this.acceptTask;
                                break;
                            }

                            this.joinEvent.WaitOne(10000);
                            while (this.listening)
                            {
                                this.token.ThrowIfCancellationRequested();
                                if (this.Connections.Count == 0 || this.Sessions.Count == 0)
                                {
                                    break;
                                }

                                // Calculate the multicast payload for the open sessions and transmit a multicast wave.
                                ICollection<MulticastConnection> waveConnections = await this.CleanupConnections();
                                ICollection<MulticastSession> waveSessions = this.CalculatePayloadAndCleanupSessions(waveConnections);
                                this.token.ThrowIfCancellationRequested();
                                await this.TransmitUdp(waveSessions, waveConnections);
                                await this.WaveUpdate();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log.Error(ex);
                    if (!(ex is SessionAbortedException))
                    {
                        throw new SessionAbortedException(Resources.ServerAborted, ex);
                    }

                    throw;
                }
                finally
                {
                    this.listening = false;
                }
            });
        }

        /// <summary>
        /// Closes this instance and any connections associated with it.
        /// </summary>
        /// <returns>A task object.</returns>
        public async Task Close()
        {
            await this.listener.Close();
            foreach (MulticastConnection conn in this.Connections)
            {
                await conn.Close();
            }

            if (this.acceptTask != null)
            {
                await this.acceptTask;
            }
        }

        internal async Task AcceptAndJoinClients(CancellationToken token)
        {
            this.token = token;
            this.listening = true;
            await this.listener.Listen();
            while (this.listening)
            {
                ICollection<ServerConnection> conns = await this.listener.ReceiveConnections(this.token);
                foreach (ServerConnection conn in conns)
                {
                    using (CancellationTokenSource timeoutCts = new CancellationTokenSource())
                    using (CancellationTokenRegistration sessionTokenCanceller = this.token.Register(() => timeoutCts.Cancel()))
                    using (CancellationTokenRegistration timeoutCancellationTokenCanceller = timeoutCts.Token.Register(() => timeoutCts.Cancel()))
                    {
                        CancellationToken timeoutToken = timeoutCts.Token;
                        MulticastConnection mcConn = new MulticastConnection(this, conn);
                        string path;
                        try
                        {
                            timeoutCts.CancelAfter(ResponseDelay);
                            path = await mcConn.AcceptConnection(timeoutToken);

                            MulticastSession session = await this.GetSession(path);

                            timeoutCts.CancelAfter(ResponseDelay);
                            await mcConn.JoinSession(session, timeoutToken);
                        }
                        catch (Exception ex)
                        {
                            this.log.Warn("Client connection failed");
                            this.log.Warn(ex.Message);
                            await mcConn.SessionJoinFailed(timeoutToken, ex);
                            continue;
                        }

                        lock (this.connectionLock)
                        {
                            if (this.connections.Contains(mcConn))
                            {
                                throw new InvalidOperationException(Resources.AttemptToAddDuplicateConnection);
                            }

                            this.connections.Add(mcConn);
                            this.joinEvent.Set();
                        }
                    }
                }
            }
        }

        internal async Task<ICollection<MulticastConnection>> CleanupConnections()
        {
            ICollection<MulticastConnection> connections = this.Connections;
            List<MulticastConnection> newConns = new List<MulticastConnection>(connections.Count);
            DateTime now = DateTime.Now;
            foreach (MulticastConnection conn in connections)
            {
                if (conn.LeavingSession || conn.WhenExpires < now)
                {
                    await conn.Close();
                    conn.Dispose();
                }
                else
                {
                    newConns.Add(conn);
                }
            }

            this.connections = newConns;
            return newConns;
        }

        internal ICollection<MulticastSession> CalculatePayloadAndCleanupSessions(ICollection<MulticastConnection> connections)
        {
            HashSet<MulticastSession> activeSessions = new HashSet<MulticastSession>();
            foreach (MulticastConnection conn in connections)
            {
                activeSessions.Add(conn.Session);
            }

            lock (this.sessionLock)
            {
                this.sessions.AsParallel().ForAll(async (ms) =>
                {
                    if (!activeSessions.Contains(ms))
                    {
                        await ms.Close();
                        ms.Dispose();
                    }

                    ms.CalculatePayload(connections);
                });

                this.sessions = activeSessions.ToList();

                long waveRemaining = 0;
                long waveTotal = 0;
                foreach (MulticastSession session in this.sessions)
                {
                    waveRemaining += session.BytesRemaining;
                    waveTotal += session.TotalBytes;
                }

                this.activeSession = null;
                this.log.DebugFormat(CultureInfo.InvariantCulture, "Bytes remaining: {0}", waveRemaining);
                this.log.DebugFormat(CultureInfo.InvariantCulture, "Bytes total: {0}", waveTotal);
                return this.sessions;
            }
        }

        internal async Task WaveUpdate()
        {
            await this.RespondToClients<WaveStatusUpdate, WaveCompleteResponse>((wsu, mc, token) =>
            {
                DateTime when = DateTime.Now;
                mc.UpdateWaveStatus(wsu, when, token);
                WaveCompleteResponse response = new WaveCompleteResponse();
                response.ResponseType = ResponseId.Ok;
                response.WaveNumber = mc.Session.WaveNumber;
                return response;
            });
        }

        internal async Task TransmitUdp(ICollection<MulticastSession> sessions, ICollection<MulticastConnection> connections)
        {
            using (CancellationTokenSource statusCts = new CancellationTokenSource())
            using (CancellationTokenRegistration statusCancellationTokenCanceller = statusCts.Token.Register(() => statusCts.Cancel()))
            {
                Task statusUpdate = this.ProcessStatusUpdates(connections, statusCts.Token);
                foreach (MulticastSession session in sessions)
                {
                    this.token.ThrowIfCancellationRequested();
                    this.activeSession = session;
                    await session.TransmitWave(this.token);
                }

                statusCts.Cancel();
                await statusUpdate;
            }
        }

        internal async Task ProcessStatusUpdates(ICollection<MulticastConnection> connections, CancellationToken updateToken)
        {
            bool waveComplete = false;
            using (CancellationTokenRegistration registration = updateToken.Register(() => waveComplete = true))
            {
                bool finalUpdate = false;
                while (this.listening && !finalUpdate)
                {
                    if (waveComplete)
                    {
                        finalUpdate = true;
                    }

                    await this.RespondToClients<PacketStatusUpdate, PacketStatusUpdateResponse>((psu, mc, token) =>
                    {
                        DateTime when = DateTime.Now;
                        mc.UpdatePacketStatus(psu, when, token);
                        PacketStatusUpdateResponse response = new PacketStatusUpdateResponse();
                        response.ResponseType = finalUpdate ? ResponseId.WaveComplete : ResponseId.Ok;
                        response.ReceptionRate = mc.ReceptionRate;
                        return response;
                    });

                    this.UpdateBurstDelay(connections);
                }
            }
        }

        internal void UpdateBurstDelay(ICollection<MulticastConnection> connections)
        {
            SortedSet<double> receptionRates = new SortedSet<double>();
            foreach (MulticastConnection conn in connections)
            {
                this.log.Trace(conn + " bps: " + conn.BytesPerSecond + ", rr: " + conn.ReceptionRate);
                receptionRates.Add(conn.ReceptionRate);
            }

            double receptionRate;
            switch (this.ServerSettings.DelayCalculation)
            {
                case DelayCalculation.MinimumThroughput:
                    receptionRate = receptionRates.Min;
                    break;
                case DelayCalculation.MaximumThroughput:
                    receptionRate = receptionRates.Max;
                    break;
                case DelayCalculation.AverageThroughput:
                    receptionRate = receptionRates.Average();
                    break;
                default:
                    throw new NotImplementedException();
            }

            this.receptionRate = new BoxedDouble(receptionRate);
            int burstDelay = this.BurstDelayMs;
            if (receptionRate < IncreaseThreshold || this.BytesPerSecond > this.ServerSettings.MaxBytesPerSecond)
            {
                ++burstDelay;
            }
            else if (receptionRate > DecreaseThreshold)
            {
                --burstDelay;
            }

            if (burstDelay < MinBurstDelay)
            {
                burstDelay = MinBurstDelay;
            }
            else if (burstDelay > MaxBurstDelay)
            {
                burstDelay = MaxBurstDelay;
            }

            this.BurstDelayMs = burstDelay;
            this.log.Debug("Server reception rate: " + receptionRate + ", burst delay: " + burstDelay);
        }

        internal async Task<MulticastSession> GetSession(string path)
        {
            IFolder rootFolder = this.Settings.RootFolder;
            ExistenceCheckResult pathExists = await rootFolder.CheckExistsAsync(path);
            string key;
            if (pathExists == ExistenceCheckResult.FileExists)
            {
                key = (await rootFolder.GetFileAsync(path)).Path;
            }
            else if (pathExists == ExistenceCheckResult.FolderExists)
            {
                key = (await rootFolder.GetFolderAsync(path)).Path;
            }
            else
            {
                throw new FileNotFoundException();
            }

            HashSet<int> usedSessions = new HashSet<int>();
            foreach (MulticastSession session in this.Sessions)
            {
                if (session.Path == key)
                {
                    return session;
                }

                usedSessions.Add(session.SessionId);
            }

            if (usedSessions.Count >= this.ServerSettings.MaxSessions)
            {
                throw new InvalidOperationException(Resources.TooManySessions);
            }

            for (int i = 0; i < this.ServerSettings.MaxSessions; ++i)
            {
                if (!usedSessions.Contains(i))
                {
                    MulticastSession newSession = new MulticastSession(this, key, i);
                    UdpWriter writer = await this.listener.CreateWriter(this.udpMulticast, i, this.encoderFactory);
                    await newSession.StartSession(writer, this.token);
                    lock (this.sessionLock)
                    {
                        this.sessions.Add(newSession);
                        return newSession;
                    }
                }
            }

            throw new InvalidOperationException();
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
                    foreach (MulticastSession session in this.sessions)
                    {
                        session.Dispose();
                    }

                    this.sessions.Clear();

                    foreach (MulticastConnection connection in this.connections)
                    {
                        connection.Dispose();
                    }

                    this.connections.Clear();

                    if (this.listener != null)
                    {
                        this.listener.Dispose();
                        this.listener = null;
                    }

                    if (this.joinEvent != null)
                    {
                        this.joinEvent.Dispose();
                    }
                }

                this.sessions = null;
                this.connections = null;
            }
        }

        // Read a request from each client, and send a response back. Completes when all clients are responded to, or when the built-in response delay completes.
        private Task RespondToClients<TRequest, TResponse>(Func<TRequest, MulticastConnection, CancellationToken, TResponse> clientAction)
            where TRequest : class
            where TResponse : class
        {
            return Task.Run(() =>
            {
                ICollection<MulticastConnection> conns = this.Connections;
                object listLock = new object();
                List<MulticastConnection> failedConnections = new List<MulticastConnection>();
                using (CancellationTokenSource timeoutCts = new CancellationTokenSource(ResponseDelay))
                using (CancellationTokenRegistration sessionTokenCanceller = this.token.Register(() => timeoutCts.Cancel()))
                using (CancellationTokenRegistration timeoutCancellationTokenCanceller = timeoutCts.Token.Register(() => timeoutCts.Cancel()))
                {
                    CancellationToken timeoutToken = timeoutCts.Token;
                    conns.AsParallel().ForAll(async (mc) =>
                    {
                        if (!mc.LeavingSession)
                        {
                            try
                            {
                                TRequest request = await mc.ServerConnection.Receive<TRequest>(timeoutToken);
                                TResponse response = clientAction(request, mc, timeoutToken);
                                await mc.ServerConnection.Send(response, timeoutToken);
                            }
                            catch (Exception ex)
                            {
                                lock (listLock)
                                {
                                    this.log.Warn("Connection failed: " + mc);
                                    this.log.Warn(ex);
                                    mc.Dispose();
                                    failedConnections.Add(mc);
                                }
                            }
                        }
                    });

                    if (failedConnections.Count > 0)
                    {
                        lock (this.connectionLock)
                        {
                            foreach (MulticastConnection conn in failedConnections)
                            {
                                this.connections.Remove(conn);
                            }
                        }
                    }
                }
            });
        }

        private void CancelListen()
        {
            this.listening = false;
            this.joinEvent.Set();
        }
    }
}
