// <copyright file="ServerListener.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.IO;
    using Cryptography;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerListener : ServerConnectionBase
    {
        private ILog log = LogManager.GetLogger<ServerListener>();
        private TcpSocketListener listener = new TcpSocketListener();
        private AutoResetEvent clientConnectedEvent = new AutoResetEvent(false);
        private ConcurrentQueue<ITcpSocketClient> connections = new ConcurrentQueue<ITcpSocketClient>();
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

        internal async Task InitServerListener()
        {
            ICommsInterface commsInterface = await this.GetCommsInterface();
            this.log.DebugFormat("Listening on {0}, if={1}", this.UriParameters, commsInterface != null ? commsInterface.Name : string.Empty);
            await this.listener.StartListeningAsync(this.UriParameters.Port, commsInterface);
            this.tcpListen = true;
            this.listener.ConnectionReceived += this.SocketConnectionRecieved;
        }

        internal async Task<ServerUdpBroadcaster> CreateBroadcaster(string multicastAddress, int sessionId)
        {
            ServerUdpBroadcaster broadcaster = new ServerUdpBroadcaster(this.UriParameters, this.Settings, this.ServerSettings);
            await broadcaster.InitMulticastServer(multicastAddress, this.ServerSettings.MulticastStartPort + sessionId);
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

        internal async Task<ICollection<ServerConnection>> ReceiveListeners(CancellationToken token)
        {
            Task<List<ServerConnection>> t0 = Task.Run(() =>
            {
                this.clientConnectedEvent.WaitOne();
                List<ServerConnection> ret = new List<ServerConnection>();
                ITcpSocketClient client;
                while (this.connections.TryDequeue(out client))
                {
                    token.ThrowIfCancellationRequested();
                    this.log.DebugFormat("Accepting connection from {0}:{1}", client.RemoteAddress, client.RemotePort);
                    ret.Add(new ServerConnection(this.UriParameters, this.Settings, client));
                }

                return ret;
            });

            return await t0.WaitWithCancellation(token);
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
                    if (this.listener != null)
                    {
                        this.listener.Dispose();
                    }

                    if (this.clientConnectedEvent != null)
                    {
                        this.clientConnectedEvent.Dispose();
                    }

                    ITcpSocketClient client;
                    while (this.connections.TryDequeue(out client))
                    {
                        client.Dispose();
                    }
                }
            }
        }

        private void SocketConnectionRecieved(object sender, TcpSocketListenerConnectEventArgs eventArgs)
        {
            while (this.connections.Count >= this.ServerSettings.MaxPendingConnections)
            {
                ITcpSocketClient client;
                if (this.connections.TryDequeue(out client))
                {
                    client.Dispose();
                }
            }

            this.connections.Enqueue(eventArgs.SocketClient);
            this.clientConnectedEvent.Set();
        }
    }
}
