// <copyright file="ClientUdpReader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ClientUdpReader : ConnectionBase
    {
        private ILog log = LogManager.GetLogger<ClientUdpReader>();
        private UdpSocketMulticastClient multicastClient = new UdpSocketMulticastClient();
        private int bufferUse = 0;
        private ConcurrentQueue<byte[]> multicastPackets = new ConcurrentQueue<byte[]>();
        private ConcurrentBag<IEncoder> encoders = new ConcurrentBag<IEncoder>();
        private AutoResetEvent packetQueuedEvent = new AutoResetEvent(false);
        private bool udpListen;
        private bool disposed;

        internal ClientUdpReader(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal UdpSocketMulticastClient MulticastClient
        {
            get
            {
                return this.multicastClient;
            }
        }

        internal async Task InitMulticastClient(string multicastAddress, int port)
        {
            this.udpListen = true;
            this.multicastClient.TTL = this.Settings.Ttl;
            this.MulticastClient.MessageReceived += this.MulticastPacketReceived;
            await this.multicastClient.JoinMulticastGroupAsync(multicastAddress, port);
        }

        internal override async Task Close()
        {
            await base.Close();
            if (this.udpListen)
            {
                this.MulticastClient.MessageReceived -= this.MulticastPacketReceived;
                await this.multicastClient.DisconnectAsync();
            }
        }

        internal async Task<ICollection<T>> ReceiveMulticast<T>(CancellationToken token)
        {
            Task<List<T>> t0 = Task.Run(() =>
            {
                int received = 0;
                int bufferSize = this.Settings.MulticastBufferSize;
                IEncoderFactory encoderFactory = this.Settings.Encoder;
                List<byte[]> pendingDeserializes = new List<byte[]>(this.multicastPackets.Count);
                this.packetQueuedEvent.WaitOne();
                byte[] next;
                while (this.multicastPackets.TryDequeue(out next))
                {
                    token.ThrowIfCancellationRequested();
                    pendingDeserializes.Add(next);
                    received += next.Length;
                    if (received >= bufferSize)
                    {
                        break;
                    }
                }

                List<T> ret = pendingDeserializes.AsParallel().Select((d) =>
                {
                    if (encoderFactory != null)
                    {
                        IEncoder encoder;
                        if (!this.encoders.TryTake(out encoder))
                        {
                            encoder = encoderFactory.CreateEncoder();
                        }

                        next = encoder.Decode(next);
                        this.encoders.Add(encoder);
                    }

                    using (MemoryStream ms = new MemoryStream(next))
                    {
                        T val = Serializer.Deserialize<T>(ms);
                        return val;
                    }
                }).ToList();
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
            while (this.bufferUse + args.ByteData.Length > this.Settings.MulticastBufferSize)
            {
                byte[] unused;
                this.multicastPackets.TryDequeue(out unused);
            }

            this.bufferUse += args.ByteData.Length;
            this.multicastPackets.Enqueue(args.ByteData);
            this.packetQueuedEvent.Set();
        }
    }
}
