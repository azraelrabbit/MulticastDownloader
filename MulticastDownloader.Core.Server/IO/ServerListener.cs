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

    internal class ServerListener : IDisposable
    {
        private const int MaxConnections = 1 << 12;
        private ILog log = LogManager.GetLogger<ServerListener>();
        private TcpSocketListener listener = new TcpSocketListener();
        private AutoResetEvent clientConnectedEvent = new AutoResetEvent(false);
        private ConcurrentQueue<ITcpSocketClient> connections = new ConcurrentQueue<ITcpSocketClient>();
        private bool listening;
        private bool disposed;

        internal ServerListener(UriParameters parms, IEncoder encoder, int ttl, int mtu)
        {
            Contract.Requires(parms != null);
            Contract.Requires(encoder != null);
            Contract.Requires(ttl > 0);
            Contract.Requires(ttl >= 576);
            this.UriParameters = parms;
            this.Encoder = encoder;
            this.Ttl = ttl;
            this.Mtu = mtu;
        }

        internal UriParameters UriParameters
        {
            get;
            private set;
        }

        internal int Ttl
        {
            get;
            private set;
        }

        internal int Mtu
        {
            get;
            private set;
        }

        internal IEncoder Encoder
        {
            get;
            private set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        internal async Task InitServerListener()
        {
            this.log.DebugFormat("Listening on {0}", this.UriParameters);
            await this.listener.StartListeningAsync(this.UriParameters.Port);
            this.listening = true;
            this.listener.ConnectionReceived += this.SocketConnectionRecieved;
        }

        internal async Task Close()
        {
            this.log.DebugFormat("Closing server listener...");
            if (this.listening)
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
                    ret.Add(new ServerConnection(this.UriParameters, this.Encoder, client, this.Ttl, this.Mtu));
                }

                return ret;
            });

            return await t0.WaitWithCancellation(token);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
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
            while (this.connections.Count >= MaxConnections)
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
