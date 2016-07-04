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
    using System.Linq;
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
        private ConcurrentBag<IEncoder> encoders = new ConcurrentBag<IEncoder>();
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

        internal async Task SendMulticast<T>(IEnumerable<T> data, CancellationToken token)
        {
            Contract.Requires(data != null);
            Task t0 = Task.Run(() =>
            {
                IEncoderFactory encoderFactory = this.Settings.Encoder;
                data.AsParallel().ForAll(async (val) =>
                {
                    byte[] serialized = new byte[this.BlockSize];
                    using (MemoryStream ms = new MemoryStream(serialized, true))
                    {
                        Serializer.Serialize(ms, val);
                    }

                    if (encoderFactory != null)
                    {
                        IEncoder encoder;
                        if (!this.encoders.TryTake(out encoder))
                        {
                            encoder = encoderFactory.CreateEncoder();
                        }

                        serialized = encoder.Encode(serialized);
                        this.encoders.Add(encoder);
                    }

                    await this.multicastClient.SendMulticastAsync(serialized);
                });
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
