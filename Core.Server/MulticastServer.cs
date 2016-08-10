// <copyright file="MulticastServer.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using System.Collections.Concurrent;
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
        private ILog log = LogManager.GetLogger<MulticastServer>();
        private object connectionLock = new object();
        private object sessionLock = new object();
        private List<MulticastConnection> connections = new List<MulticastConnection>();
        private List<MulticastSession> sessions = new List<MulticastSession>();
        private MulticastSession activeSession;
        private ServerListener listener;
        private IUdpMulticastFactory udpMulticast;
        private HashEncoderFactory encoderFactory;
        private BoxedDouble receptionRate = new BoxedDouble(1.0);
        private CancellationToken token = CancellationToken.None;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastServer"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="serverSettings">The server settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastServer(Uri path, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : this(PortableUdpMulticast.Factory, path, settings, serverSettings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastServer"/> class.
        /// </summary>
        /// <param name="udpMulticast">The <see cref="IUdpMulticastFactory"/> implementation.</param>
        /// <param name="path">The server path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="serverSettings">The server settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastServer(IUdpMulticastFactory udpMulticast, Uri path, IMulticastSettings settings, IMulticastServerSettings serverSettings)
        {
            if (udpMulticast == null)
            {
                throw new ArgumentNullException("udpMulticast");
            }

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
            this.BurstDelayMs = ServerConstants.StartBurstDelay;
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

        internal MulticastSession ActiveSession
        {
            get
            {
                return this.activeSession;
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
                await this.listener.Listen();
                try
                {
                    for (; ;)
                    {
                        this.token.ThrowIfCancellationRequested();
                        while (this.Connections.Count == 0)
                        {
                            await Task.Delay(Constants.PacketUpdateInterval, this.token);
                            await this.AcceptAndJoinClients(false, this.token);
                        }

                        while (this.Connections.Count > 0)
                        {
                            // Calculate the multicast payload for the open sessions and transmit a multicast wave.
                            this.token.ThrowIfCancellationRequested();
                            ICollection<MulticastConnection> waveConnections = await this.CleanupConnections();
                            ICollection<MulticastSession> waveSessions = this.CalculatePayloadAndCleanupSessions(waveConnections);
                            if (waveConnections.Count > 0)
                            {
                                Contract.Assert(waveSessions.Count > 0);
                                await this.TransmitUdp(waveSessions, waveConnections);
                                await this.WaveStatusUpdate(waveConnections);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
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
            });
        }

        /// <summary>
        /// Closes this instance and any connections associated with it.
        /// </summary>
        /// <returns>A task object.</returns>
        public async Task Close()
        {
            await this.listener.Close();
            foreach (MulticastConnection mc in this.Connections)
            {
                this.log.Debug("Closing connection " + mc);
                await mc.Close();
            }
        }

        internal async Task AcceptAndJoinClients(bool listen, CancellationToken token)
        {
            if (listen)
            {
                this.token = token;
                await this.listener.Listen();
            }

            ICollection<ServerConnection> conns = this.listener.ReceiveConnections(this.token);
            foreach (ServerConnection conn in conns)
            {
                MulticastConnection mcConn = new MulticastConnection(this, conn);
                string path;
                try
                {
                    path = mcConn.AcceptConnection(this.token);
                    MulticastSession session = await this.GetSession(path);
                    mcConn.JoinSession(session, this.token);
                }
                catch (Exception ex)
                {
                    this.log.Warn("Client connection failed");
                    this.log.Warn(ex.Message);
                    try
                    {
                        mcConn.SessionJoinFailed(this.token, ex);
                    }
                    catch (Exception)
                    {
                    }

                    continue;
                }

                lock (this.connectionLock)
                {
                    if (this.connections.Contains(mcConn))
                    {
                        throw new InvalidOperationException(Resources.AttemptToAddDuplicateConnection);
                    }

                    this.connections.Add(mcConn);
                }
            }
        }

        internal async Task<ICollection<MulticastConnection>> CleanupConnections()
        {
            List<MulticastConnection> toRemove = new List<MulticastConnection>();
            try
            {
                lock (this.connectionLock)
                {
                    List<MulticastConnection> newConns = new List<MulticastConnection>(this.connections.Count);
                    DateTime now = DateTime.Now;
                    foreach (MulticastConnection conn in this.connections)
                    {
                        if (conn.LeavingSession || conn.WhenExpires < now)
                        {
                            this.log.Debug("Removing connection " + conn);
                            toRemove.Add(conn);
                        }
                        else
                        {
                            newConns.Add(conn);
                        }
                    }

                    this.connections = newConns;
                    return newConns;
                }
            }
            finally
            {
                foreach (MulticastConnection conn in toRemove)
                {
                    await conn.Close();
                    conn.Dispose();
                }
            }
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

        internal async Task TransmitUdp(ICollection<MulticastSession> sessions, ICollection<MulticastConnection> connections)
        {
            using (AutoResetEvent sessionCompleteEvent = new AutoResetEvent(false))
            {
                foreach (MulticastSession session in sessions)
                {
                    Func<long, Task> createBackgroundTask = (l) =>
                    {
                        return Task.Run(async () =>
                        {
                            sessionCompleteEvent.WaitOne(Constants.PacketUpdateInterval);
                            await this.AcceptAndJoinClients(false, this.token);
                            await this.PacketStatusUpdate(connections, false, this.token);
                            session.UpdateSessionStatus(l);
                        });
                    };
                    Func<Task<ICollection<FileSegment>>> createReadTask = () => session.ReadBurst();
                    Func<ICollection<FileSegment>, Task<long>> createWriteTask = (fs) => session.WriteBurst(fs);

                    this.activeSession = session;
                    session.BeginBurst();
                    long seq = 0;
                    long end = session.Written.RawBits.LongCount();
                    Task backgroundTask = createBackgroundTask(seq);
                    Task<ICollection<FileSegment>> readTask = createReadTask();
                    Task<long> writeTask = Task.FromResult<long>(0);
                    while (seq < end)
                    {
                        Task waited = await Task.WhenAny(backgroundTask, readTask);
                        if (waited == backgroundTask)
                        {
                            await backgroundTask;
                            if (seq < end)
                            {
                                backgroundTask = createBackgroundTask(seq);
                            }
                        }
                        else if (waited == readTask)
                        {
                            ICollection<FileSegment> toWrite = await readTask;
                            readTask = createReadTask();
                            seq = await writeTask;
                            writeTask = createWriteTask(toWrite);
                        }
                    }

                    sessionCompleteEvent.Set();
                    await Task.WhenAll(backgroundTask, readTask, writeTask);
                }
            }

            await this.PacketStatusUpdate(connections, true, this.token);
        }

        internal async Task PacketStatusUpdate(ICollection<MulticastConnection> connections, bool waveComplete, CancellationToken waveCompleteToken)
        {
            long bytesLeft = this.activeSession.BytesRemaining;
            await this.RespondToClients<PacketStatusUpdate, PacketStatusUpdateResponse>(connections, (psu, mc) =>
            {
                mc.UpdatePacketStatus(psu, DateTime.Now);
                PacketStatusUpdateResponse response = new PacketStatusUpdateResponse();
                response.ResponseType = waveComplete ? ResponseId.WaveComplete : ResponseId.Ok;
                response.ReceptionRate = mc.ReceptionRate;
                return response;
            });

            this.UpdateBurstDelay(connections);
        }

        internal async Task WaveStatusUpdate(ICollection<MulticastConnection> connections)
        {
            await this.RespondToClients<WaveStatusUpdate, WaveCompleteResponse>(connections, (wsu, mc) =>
            {
                mc.UpdateWaveStatus(wsu, DateTime.Now);
                WaveCompleteResponse response = new WaveCompleteResponse();
                response.ResponseType = ResponseId.Ok;
                response.WaveNumber = mc.Session.WaveNumber;
                return response;
            });
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
            if (receptionRate < ServerConstants.IncreaseThreshold || this.BytesPerSecond > this.ServerSettings.MaxBytesPerSecond)
            {
                ++burstDelay;
            }
            else if (receptionRate > ServerConstants.DecreaseThreshold)
            {
                --burstDelay;
            }

            if (burstDelay < ServerConstants.MinBurstDelay)
            {
                burstDelay = ServerConstants.MinBurstDelay;
            }
            else if (burstDelay > ServerConstants.MaxBurstDelay)
            {
                burstDelay = ServerConstants.MaxBurstDelay;
            }

            this.BurstDelayMs = burstDelay;
            this.log.Debug("Server reception rate: " + receptionRate + ", burst delay: " + burstDelay + ", conns: " + connections.Count);
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
                    UdpWriter writer = await this.listener.CreateWriter(this.udpMulticast.CreateMulticast(), i, this.encoderFactory);
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
                    }
                }
            }
        }

        // Read a request from each client, and send a response back. Completes when all clients are responded to, or when the built-in response delay completes.
        private Task RespondToClients<TRequest, TResponse>(ICollection<MulticastConnection> connections, Func<TRequest, MulticastConnection, TResponse> clientAction)
            where TRequest : class
            where TResponse : class
        {
            return Task.Run(() =>
            {
                if (connections.Count > 0)
                {
                    connections.AsParallel().ForAll((mc) =>
                    {
                        if (!mc.LeavingSession)
                        {
                            try
                            {
                                TRequest request = mc.ServerConnection.Receive<TRequest>(this.token);
                                TResponse response = clientAction(request, mc);
                                if (!mc.LeavingSession)
                                {
                                    mc.ServerConnection.Send(response, this.token);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.log.Warn("Connection failed: " + mc);
                                this.log.Warn(ex);
                                mc.LeavingSession = true;
                            }
                        }
                    });
                }
            });
        }
    }
}
