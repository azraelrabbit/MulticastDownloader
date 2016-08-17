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
    using Cryptography;
    using ProtoBuf;
    using Session;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class UdpWriter : ServerConnectionBase
    {
        private ILog log = LogManager.GetLogger<UdpWriter>();
        private IEncoderFactory encoderFactory;
        private IUdpMulticast multicastClient;
        private bool disposed;

        internal UdpWriter(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings, IUdpMulticast udpMulticast)
            : base(parms, settings, serverSettings)
        {
            if (udpMulticast == null)
            {
                throw new ArgumentNullException("udpMulticast");
            }

            this.multicastClient = udpMulticast;
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
                this.encoderFactory = encoderFactory;
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

            await this.multicastClient.Connect(commsInterface != null ? commsInterface.Name : null, this.MulticastAddress, this.MulticastPort, this.Settings.Ttl);
        }

        internal override async Task Close()
        {
            await base.Close();
            await this.multicastClient.Close();
        }

        internal async Task SendMulticast<T>(IEnumerable<T> data)
        {
            Contract.Requires(data != null);
            IEnumerable<Task> t0 = data.Select(async (val) =>
            {
                using (MemoryStream ms = new MemoryStream(this.BlockSize))
                {
                    Serializer.Serialize(ms, val);
                    byte[] serialized = new byte[ms.Position];
                    Array.Copy(ms.ToArray(), serialized, serialized.Length);
                    if (this.encoderFactory != null)
                    {
                        IEncoder encoder = this.encoderFactory.CreateEncoder();
                        serialized = encoder.Encode(serialized);
                    }

                    Contract.Assert(serialized.Length <= this.ServerSettings.Mtu);
                    await this.multicastClient.Send(serialized);
                }
            });

            await Task.WhenAll(t0);
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
