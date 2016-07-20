// <copyright file="ServerListener.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.Cryptography;
    using Core.IO;
    using Cryptography;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Tls;
    using Org.BouncyCastle.Security;
    using PCLStorage;
    using Properties;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerListener : ServerConnectionBase
    {
        private ILog log = LogManager.GetLogger<ServerListener>();
        private TcpSocketListener listener = new TcpSocketListener();
        private AutoResetEvent clientConnectedEvent = new AutoResetEvent(false);
        private ConcurrentQueue<ServerConnection> connections = new ConcurrentQueue<ServerConnection>();
        private bool tcpListen;
        private bool disposed;

        internal ServerListener(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : base(parms, settings, serverSettings)
        {
        }

        internal TcpSocketListener Listener
        {
            get
            {
                return this.listener;
            }
        }

        internal async Task Listen()
        {
            ICommsInterface commsInterface = await this.GetCommsInterface();
            this.log.DebugFormat("Listening on {0}, if={1}", this.UriParameters, commsInterface != null ? commsInterface.Name : string.Empty);
            await this.listener.StartListeningAsync(this.UriParameters.Port, commsInterface);
            this.tcpListen = true;
            this.listener.ConnectionReceived += this.SocketConnectionRecieved;
        }

        internal async Task<UdpWriter> CreateWriter(int sessionId, IEncoderFactory encoder)
        {
            UdpWriter broadcaster = new UdpWriter(this.UriParameters, this.Settings, this.ServerSettings);
            await broadcaster.StartMulticastServer(sessionId, encoder);
            return broadcaster;
        }

        internal override async Task Close()
        {
            this.log.DebugFormat("Closing server listener...");
            if (this.tcpListen)
            {
                this.listener.ConnectionReceived -= this.SocketConnectionRecieved;
                await this.listener.StopListeningAsync();
            }
        }

        internal async Task<ICollection<ServerConnection>> ReceiveConnections(CancellationToken token)
        {
            Task<List<ServerConnection>> t0 = Task.Run(() =>
            {
                this.clientConnectedEvent.WaitOne(1000);
                List<ServerConnection> ret = new List<ServerConnection>();
                ServerConnection conn;
                while (this.connections.TryDequeue(out conn))
                {
                    token.ThrowIfCancellationRequested();
                    this.log.DebugFormat("Accepting connection from {0}:{1}", conn.TcpSession.RemoteAddress, conn.TcpSession.RemotePort);
                    ret.Add(conn);
                }

                return ret;
            });

            return await t0.WaitWithCancellation(token);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        //// Listener can't be disposed as there's a chance it'll throw an ODE (this is probably a bug in TcpListener)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "listener", Justification = "none")]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.clientConnectedEvent != null)
                    {
                        this.clientConnectedEvent.Dispose();
                    }

                    ServerConnection conn;
                    while (this.connections.TryDequeue(out conn))
                    {
                        conn.Dispose();
                    }
                }
            }
        }

        private void SocketConnectionRecieved(object sender, TcpSocketListenerConnectEventArgs eventArgs)
        {
            while (this.connections.Count >= this.ServerSettings.MaxConnections)
            {
                ServerConnection conn;
                if (this.connections.TryDequeue(out conn))
                {
                    conn.Dispose();
                }
            }

            try
            {
                ITcpSocketClient client = eventArgs.SocketClient;
                ServerConnection conn = new ServerConnection(this.UriParameters, this.Settings);
                conn.SetTcpSession(client);
                this.clientConnectedEvent.Set();
                this.connections.Enqueue(conn);
            }
            catch (GeneralSecurityException gse)
            {
                this.log.Error("Authentication failed");
                this.log.Error(gse);
            }
        }
    }
}
