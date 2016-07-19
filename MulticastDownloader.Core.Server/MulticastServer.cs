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
    using Core.Session;
    using Cryptography;
    using IO;
    using Org.BouncyCastle.Security;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent a multicast server.
    /// </summary>
    /// <seealso cref="ServerBase" />
    /// <seealso cref="MulticastConnection" />
    /// <seealso cref="MulticastSession" />
    public class MulticastServer : ServerBase
    {
        // Clients much respond to any request with the given interval
        internal static readonly TimeSpan ResponseDelay = TimeSpan.FromSeconds(60);
        private ILog log = LogManager.GetLogger<MulticastServer>();
        private object connectionLock = new object();
        private object sessionLock = new object();
        private List<MulticastConnection> connections = new List<MulticastConnection>();
        private List<MulticastSession> sessions = new List<MulticastSession>();
        private AutoResetEvent joinEvent = new AutoResetEvent(false);
        private ServerListener listener;
        private HashEncoderFactory encoderFactory;
        private CancellationToken token = CancellationToken.None;
        private bool listening;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastServer"/> class.
        /// </summary>
        /// <param name="path">The server path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="serverSettings">The server settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastServer(Uri path, IMulticastSettings settings, IMulticastServerSettings serverSettings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (serverSettings == null)
            {
                throw new ArgumentNullException("serverSettings");
            }

            this.Parameters = new UriParameters(path);
            this.Settings = settings;
            this.ServerSettings = serverSettings;
            this.listener = new ServerListener(this.Parameters, settings, serverSettings);
            SecureRandom sr = new SecureRandom();
            this.ChallengeKey = new byte[MulticastClient.EncoderSize / 8];
            sr.NextBytes(this.ChallengeKey);
            this.encoderFactory = new HashEncoderFactory(this.ChallengeKey, MulticastClient.EncoderSize);
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
                    await this.listener.Listen();
                    Task acceptTask = this.AcceptAndJoinClients();
                    this.listening = true;
                    using (CancellationTokenRegistration registration = this.token.Register(this.CancelListen))
                    {
                        while (this.listening)
                        {
                            if (acceptTask.IsFaulted || acceptTask.IsCanceled || acceptTask.IsCompleted)
                            {
                                // We may have gotten hanked.
                                await acceptTask;
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
                                IDictionary<int, ICollection<MulticastConnection>> connectionsBySessionId = await this.CleanupConnections();
                                ICollection<MulticastSession> waveSessions = this.CalculatePayloadAndCleanupSessions(connectionsBySessionId);
                                this.token.ThrowIfCancellationRequested();
                                await this.TransmitUdp(waveSessions);
                                await this.WaveUpdate();
                            }
                        }
                    }

                    await acceptTask;
                    await this.listener.Close();
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

                    foreach (MulticastConnection connection in this.connections)
                    {
                        connection.Dispose();
                    }

                    if (this.listener != null)
                    {
                        this.listener.Dispose();
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

        private async Task AcceptAndJoinClients()
        {
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
                        timeoutCts.CancelAfter(ResponseDelay);
                        try
                        {
                            path = await mcConn.AcceptConnection(timeoutToken);
                        }
                        catch (Exception ex)
                        {
                            this.log.Warn("Client connection failed");
                            this.log.Warn(ex.Message);
                            continue;
                        }

                        MulticastSession session;
                        try
                        {
                            session = await this.GetSession(path);
                        }
                        catch (Exception ex)
                        {
                            this.log.Warn("Retrieving multicast session failed: " + ex.Message);
                            this.log.Warn(ex.Message);
                            await mcConn.SessionJoinFailed(timeoutToken, ex);
                            continue;
                        }

                        timeoutCts.CancelAfter(ResponseDelay);
                        try
                        {
                            await mcConn.JoinSession(session, timeoutToken);
                        }
                        catch (Exception ex)
                        {
                            this.log.Warn("Session join failed");
                            this.log.Warn(ex.Message);
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

        private async Task<IDictionary<int, ICollection<MulticastConnection>>> CleanupConnections()
        {
            ICollection<MulticastConnection> connections = this.Connections;
            Dictionary<int, ICollection<MulticastConnection>> connectionsBySessionId = new Dictionary<int, ICollection<MulticastConnection>>(this.Sessions.Count);
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
                    ICollection<MulticastConnection> connectionBag = connectionsBySessionId[conn.Session.SessionId];
                    if (connectionBag == null)
                    {
                        connectionBag = connectionsBySessionId[conn.Session.SessionId] = new List<MulticastConnection>(this.connections.Count);
                    }

                    connectionBag.Add(conn);
                }
            }

            this.connections = newConns;
            return connectionsBySessionId;
        }

        private ICollection<MulticastSession> CalculatePayloadAndCleanupSessions(IDictionary<int, ICollection<MulticastConnection>> connectionsBySessionId)
        {
            ICollection<MulticastSession> sessions = this.Sessions;
            sessions.AsParallel().ForAll(async (ms) =>
            {
                ICollection<MulticastConnection> connections = connectionsBySessionId[ms.SessionId];
                ms.CalculatePayload(connections);
                if (ms.BytesRemaining == 0)
                {
                    lock (this.sessionLock)
                    {
                        this.sessions.Remove(ms);
                    }

                    await ms.Close();
                    ms.Dispose();
                }
            });

            long waveRemaining = 0;
            long waveTotal = 0;
            foreach (MulticastSession session in sessions)
            {
                waveRemaining += session.BytesRemaining;
                waveTotal += session.TotalBytes;
            }

            this.log.DebugFormat(CultureInfo.InvariantCulture, "Bytes remaining: {0}", waveRemaining);
            this.log.DebugFormat(CultureInfo.InvariantCulture, "Bytes total: {0}", waveTotal);
            return sessions;
        }

        private async Task ProcessStatusUpdates(CancellationToken updateToken)
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

                    await this.RespondToClients<PacketStatusUpdate, Response>((psu, mc, token) =>
                    {
                        DateTime when = DateTime.Now;
                        mc.UpdatePacketStatus(psu, when, token);
                        Response response = new Response();
                        response.ResponseType = finalUpdate ? ResponseId.WaveComplete : ResponseId.Ok;
                        return response;
                    });
                }
            }
        }

        private async Task WaveUpdate()
        {
            await this.RespondToClients<WaveStatusUpdate, WaveCompleteResponse>((wsu, mc, token) =>
            {
                DateTime when = DateTime.Now;
                mc.UpdateWaveStatus(wsu, when, token);
                WaveCompleteResponse response = new WaveCompleteResponse();
                response.ResponseType = ResponseId.Ok;
                response.WaveNumber = mc.Session.WaveNumber;
                response.ReceptionRate = mc.ReceptionRate;
                return response;
            });
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

        private async Task TransmitUdp(ICollection<MulticastSession> sessions)
        {
            using (CancellationTokenSource statusCts = new CancellationTokenSource())
            using (CancellationTokenRegistration statusCancellationTokenCanceller = statusCts.Token.Register(() => statusCts.Cancel()))
            {
                Task statusUpdate = this.ProcessStatusUpdates(statusCts.Token);
                foreach (MulticastSession session in sessions)
                {
                    this.token.ThrowIfCancellationRequested();
                    await session.TransmitWave(this.token);
                }

                statusCts.Cancel();
                await statusUpdate;
            }
        }

        private void CancelListen()
        {
            this.listening = false;
            this.joinEvent.Set();
        }

        private async Task<MulticastSession> GetSession(string path)
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
                    UdpWriter writer = await this.listener.CreateWriter(i, this.encoderFactory);
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
    }
}
