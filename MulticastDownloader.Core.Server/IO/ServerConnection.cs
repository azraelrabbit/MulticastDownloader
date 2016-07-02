// <copyright file="ServerConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System;
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

    internal class ServerConnection : ConnectionBase
    {
        private const int DefaultMtu = 576;
        private const int Ipv6Overhead = 40;
        private const int Ipv4Overhead = 20;
        private const int UdpOverhead = 8;

        private ILog log = LogManager.GetLogger<ServerConnection>();
        private int blockSize;
        private bool disposed;

        internal ServerConnection(UriParameters parms, IEncoder encoder, ITcpSocketClient client, int ttl, int mtu)
            : base(parms, encoder, ttl)
        {
            Contract.Requires(client != null);
            this.SetTcpSession(client);

            // FIXMEFIXME: Eh, we don't actually know if we're getting an IPV4 or IPV6 connection, so assume the worst...
            this.blockSize = mtu - UdpOverhead - Ipv6Overhead;
            if (this.Encoder != null)
            {
                int unencodedSize = this.blockSize;
                while (this.Encoder.GetEncodedOutputLength(unencodedSize) > this.blockSize)
                {
                    --unencodedSize;
                }

                Contract.Assert(unencodedSize > 0);
                this.blockSize = unencodedSize;
            }
        }

        internal int BlockSize
        {
            get
            {
                return this.blockSize;
            }
        }

        internal async Task SendMulticast<T>(T data, CancellationToken token)
        {
            Contract.Requires(data != null);
            Task t0 = Task.Run(async () =>
            {
                byte[] serialized = new byte[this.blockSize];
                using (MemoryStream ms = new MemoryStream(serialized, true))
                {
                    Serializer.Serialize(ms, data);
                }

                if (this.Encoder != null)
                {
                    serialized = this.Encoder.Encode(serialized);
                }

                await this.MulticastClient.SendMulticastAsync(serialized);
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
                if (this.TcpSession != null)
                {
                    this.TcpSession.Dispose();
                }
            }
        }
    }
}
