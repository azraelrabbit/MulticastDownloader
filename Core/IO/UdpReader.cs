// <copyright file="UdpReader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
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
    using Cryptography;
    using ProtoBuf;
    using Session;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class UdpReader<TReader> : ConnectionBase
        where TReader : IUdpMulticast, new()
    {
        private const int ReadDelay = 1000;
        private ILog log = LogManager.GetLogger<UdpReader<TReader>>();
        private IEncoderFactory encoder;
        private TReader multicastClient = new TReader();
        private int bufferUse = 0;
        private int bufferSize;
        private AutoResetEvent packetQueuedEvent = new AutoResetEvent(false);
        private Task readTask;
        private CancellationTokenSource readCts = new CancellationTokenSource();
        private ConcurrentQueue<byte[]> multicastPackets = new ConcurrentQueue<byte[]>();
        private ConcurrentBag<IDecoder> encoders = new ConcurrentBag<IDecoder>();
        private bool disposed;

        internal UdpReader(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal async Task JoinMulticastServer(SessionJoinResponse response, IEncoderFactory encoder)
        {
            Contract.Requires(response != null);
            this.encoder = encoder;
            this.bufferSize = this.Settings.MulticastBufferSize;
            int blockSize = SessionJoinResponse.GetBlockSize(response.Mtu, response.Ipv6);
            if (blockSize * response.MulticastBurstLength > this.bufferSize)
            {
                this.bufferSize = blockSize * response.MulticastBurstLength;
            }

            this.log.DebugFormat("UDP reader size: {0} bytes", this.bufferSize);
            await this.multicastClient.Connect(null, response.MulticastAddress, response.MulticastPort, this.Settings.Ttl);
            this.readTask = this.multicastClient.Read(this.PacketRead, this.readCts.Token);
        }

        internal override async Task Close()
        {
            await base.Close();
            this.readCts.Cancel();
            if (this.readTask != null)
            {
                try
                {
                    await this.readTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            await this.multicastClient.Close();
        }

        internal async Task<IEnumerable<T>> ReceiveMulticast<T>(CancellationToken token)
        {
            Task<IEnumerable<T>> t0 = Task.Run(() =>
            {
                int received = 0;
                int bufferSize = this.Settings.MulticastBufferSize;
                IEncoderFactory encoderFactory = this.encoder;
                List<byte[]> pendingDeserializes = new List<byte[]>(this.multicastPackets.Count);
                this.packetQueuedEvent.WaitOne(ReadDelay);
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

                IEnumerable<T> ret = pendingDeserializes.AsParallel().Select((d) =>
                {
                    if (encoderFactory != null)
                    {
                        IDecoder encoder;
                        if (!this.encoders.TryTake(out encoder))
                        {
                            encoder = encoderFactory.CreateDecoder();
                        }

                        next = encoder.Decode(next);
                        this.encoders.Add(encoder);
                    }

                    using (MemoryStream ms = new MemoryStream(next))
                    {
                        T val = Serializer.Deserialize<T>(ms);
                        return val;
                    }
                }).ToArray();
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
                if (this.readCts != null)
                {
                    this.readCts.Dispose();
                }

                if (this.multicastClient != null)
                {
                    this.multicastClient.Dispose();
                }

                if (this.packetQueuedEvent != null)
                {
                    this.packetQueuedEvent.Dispose();
                }

                this.multicastPackets = null;
            }
        }

        private void PacketRead(byte[] data)
        {
            while (this.bufferUse + data.Length > this.bufferSize)
            {
                byte[] unused;
                this.multicastPackets.TryDequeue(out unused);
            }

            this.bufferUse += data.Length;
            this.multicastPackets.Enqueue(data);
            this.packetQueuedEvent.Set();
        }
    }
}
