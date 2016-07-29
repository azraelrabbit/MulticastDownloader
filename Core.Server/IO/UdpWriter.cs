// <copyright file="UdpWriter.cs" company="MS">
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
    using Core.Cryptography;
    using Core.IO;
    using Core.Session;
    using Cryptography;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class UdpWriter<TWriter> : ServerConnectionBase
        where TWriter : IUdpMulticast, new()
    {
        private ILog log = LogManager.GetLogger<UdpWriter<TWriter>>();
        private IEncoderFactory encoder;
        private TWriter multicastClient = new TWriter();
        private ConcurrentBag<IEncoder> encoders = new ConcurrentBag<IEncoder>();
        private bool disposed;

        internal UdpWriter(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : base(parms, settings, serverSettings)
        {
        }

        internal int BlockSize
        {
            get;
            private set;
        }

        internal string MulticastAddress
        {
            get;
            private set;
        }

        internal int MulticastPort
        {
            get;
            private set;
        }

        internal bool Ipv6
        {
            get;
            private set;
        }

        internal async Task StartMulticastServer(int sessionId, IEncoderFactory encoderFactory)
        {
            Contract.Requires(sessionId >= 0 && sessionId < this.ServerSettings.MaxSessions);
            this.MulticastAddress = this.ServerSettings.MulticastAddress;
            this.MulticastPort = this.ServerSettings.MulticastStartPort + sessionId;
            this.Ipv6 = this.ServerSettings.Ipv6;
            this.BlockSize = SessionJoinResponse.GetBlockSize(this.ServerSettings.Mtu, this.Ipv6);
            if (encoderFactory != null)
            {
                this.encoder = encoderFactory;
                IEncoder encoder = encoderFactory.CreateEncoder();
                int unencodedSize = this.BlockSize;
                while (encoder.GetEncodedOutputLength(unencodedSize) > this.BlockSize)
                {
                    --unencodedSize;
                }

                Contract.Assert(unencodedSize > 0);
                this.BlockSize = unencodedSize;
            }

            ICommsInterface commsInterface = await this.GetCommsInterface();
            this.log.DebugFormat("Initializing multicast session on {0}:{1}, if={2}", this.MulticastAddress, this.MulticastPort, commsInterface != null ? commsInterface.Name : string.Empty);
            if (commsInterface != null)
            {
                this.log.DebugFormat("Interface broadcast ip={0}", commsInterface.BroadcastAddress);
                if (this.MulticastAddress != commsInterface.BroadcastAddress)
                {
                    this.log.Warn("WARNING: Check your server settings. The broadcast address of your interface doesn't match the multicast address. This could result in weird behavior.");
                }
            }

            await this.multicastClient.Connect(commsInterface.Name, this.MulticastAddress, this.MulticastPort, this.Settings.Ttl);
        }

        internal override async Task Close()
        {
            await base.Close();
            await this.multicastClient.Close();
        }

        internal async Task SendMulticast<T>(IEnumerable<T> data, CancellationToken token)
        {
            Contract.Requires(data != null);
            Task t0 = Task.Run(() =>
            {
                IEncoderFactory encoderFactory = this.encoder;
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

                    await this.multicastClient.Send(serialized);
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
