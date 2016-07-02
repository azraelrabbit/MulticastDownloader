// <copyright file="ClientConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    // Represent a wrapper around client connection state.
    internal class ClientConnection : ConnectionBase
    {
        private const int MulticastBufferSize = 1 << 20;

        private ILog log = LogManager.GetLogger<ClientConnection>();
        private ConcurrentQueue<byte[]> multicastPackets = new ConcurrentQueue<byte[]>();
        private AutoResetEvent packetQueuedEvent = new AutoResetEvent(false);
        private int multicastBufferSize = 0;
        private bool listening;
        private bool disposed;

        internal ClientConnection(UriParameters parms, IEncoder encoder, int ttl)
            : base(parms, encoder, ttl)
        {
        }

        internal async Task InitTcpClient()
        {
            this.log.DebugFormat("Connecting to {0}", this.UriParameters);
            this.SetTcpSession(new TcpSocketClient());
            await this.TcpSession.ConnectAsync(this.UriParameters.Hostname, this.UriParameters.Port, this.UriParameters.UseTls);
            this.log.DebugFormat("Connected to {0}", this.UriParameters);
        }

        internal void InitReceiveMulticast()
        {
            this.log.DebugFormat("Receiving multicast data");
            this.listening = true;
            this.MulticastClient.MessageReceived += this.MulticastPacketReceived;
        }

        internal override async Task Close()
        {
            await base.Close();
            if (this.listening)
            {
                this.MulticastClient.MessageReceived -= this.MulticastPacketReceived;
            }
        }

        internal async Task<ICollection<T>> ReceiveMulticast<T>(CancellationToken token)
        {
            Task<List<T>> t0 = Task.Run(() =>
            {
                    this.packetQueuedEvent.WaitOne();
                    List<T> ret = new List<T>();
                    byte[] next;
                    while (this.multicastPackets.TryDequeue(out next))
                    {
                        token.ThrowIfCancellationRequested();
                        if (this.Encoder != null)
                        {
                            next = this.Encoder.Decode(next);
                        }

                        using (MemoryStream ms = new MemoryStream(next))
                        {
                            T val = Serializer.Deserialize<T>(ms);
                            ret.Add(val);
                        }
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
                if (this.packetQueuedEvent != null)
                {
                    this.packetQueuedEvent.Dispose();
                }

                this.multicastPackets = null;
            }
        }

        private void MulticastPacketReceived(object sender, UdpSocketMessageReceivedEventArgs args)
        {
            while (this.multicastBufferSize - args.ByteData.Length > MulticastBufferSize)
            {
                byte[] unused;
                this.multicastPackets.TryDequeue(out unused);
            }

            this.multicastPackets.Enqueue(args.ByteData);
            this.packetQueuedEvent.Set();
        }
    }
}
