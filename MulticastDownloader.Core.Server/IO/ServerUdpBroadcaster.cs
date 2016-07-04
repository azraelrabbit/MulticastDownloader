// <copyright file="ServerUdpBroadcaster.cs" company="MS">
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
    using Core.IO;
    using Cryptography;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerUdpBroadcaster : ServerConnectionBase
    {
        private ILog log = LogManager.GetLogger<ServerListener>();
        private UdpSocketMulticastClient multicastClient = new UdpSocketMulticastClient();
        private bool udpListen;
        private bool disposed;

        internal ServerUdpBroadcaster(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : base(parms, settings, serverSettings)
        {
        }

        internal string ServerAddress
        {
            get;
            private set;
        }

        internal int ServerPort
        {
            get;
            private set;
        }

        internal UdpSocketMulticastClient MulticastClient
        {
            get
            {
                return this.multicastClient;
            }
        }

        internal async Task InitMulticastServer(string serverAddress, int port)
        {
            Contract.Requires(!string.IsNullOrEmpty(serverAddress));
            Contract.Requires(port > 0 && port >= this.ServerSettings.MulticastStartPort && port < this.ServerSettings.MulticastStartPort + this.ServerSettings.MaxSessions);
            this.udpListen = true;
            ICommsInterface commsInterface = await this.GetCommsInterface();
            this.log.DebugFormat("Initializing multicast session on {0}:{1}, if={2}", serverAddress, port, commsInterface != null ? commsInterface.Name : string.Empty);
            await this.multicastClient.JoinMulticastGroupAsync(serverAddress, port, commsInterface);
        }

        internal override async Task Close()
        {
            await base.Close();
            if (this.udpListen)
            {
                await this.multicastClient.DisconnectAsync();
            }
        }

        internal async Task SendMulticast<T>(ICollection<T> data, CancellationToken token)
        {
            Contract.Requires(data != null);
            Task t0 = Task.Run(async () =>
            {
                List<Task> pendingSends = new List<Task>(data.Count);
                IEncoder encoder = this.Settings.Encoder;
                foreach (T val in data)
                {
                    byte[] serialized = new byte[this.BlockSize];
                    using (MemoryStream ms = new MemoryStream(serialized, true))
                    {
                        Serializer.Serialize(ms, val);
                    }

                    if (encoder != null)
                    {
                        serialized = encoder.Encode(serialized);
                    }

                    pendingSends.Add(this.MulticastClient.SendMulticastAsync(serialized));
                }

                await Task.WhenAll(pendingSends);
            });

            await t0.WaitWithCancellation(token);
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
                    if (this.multicastClient != null)
                    {
                        this.multicastClient.Dispose();
                    }
                }
            }
        }
    }
}
