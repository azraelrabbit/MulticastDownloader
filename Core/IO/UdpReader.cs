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

    internal class UdpReader<T> : ConnectionBase
    {
        private ILog log = LogManager.GetLogger<UdpReader<T>>();
        private IEncoderFactory encoderFactory;
        private IUdpMulticast multicastClient;
        private int bufferUse = 0;
        private int bufferSize;
        private Task readTask;
        private CancellationTokenSource readCts = new CancellationTokenSource();
        private AutoResetEvent jobAddedEvent = new AutoResetEvent(false);
        private ConcurrentQueue<DecodeJob> pendingDecodes = new ConcurrentQueue<DecodeJob>();
        private bool disposed;

        internal UdpReader(UriParameters parms, IMulticastSettings settings, IUdpMulticast udpMulticast)
            : base(parms, settings)
        {
            if (udpMulticast == null)
            {
                throw new ArgumentNullException("udpMulticast");
            }

            this.multicastClient = udpMulticast;
        }

        internal async Task JoinMulticastServer(SessionJoinResponse response, IEncoderFactory encoder)
        {
            Contract.Requires(response != null);
            this.encoderFactory = encoder;
            this.bufferSize = this.Settings.MulticastBufferSize;
            int blockSize = SessionJoinResponse.GetBlockSize(response.Mtu, response.Ipv6);
            if (blockSize * response.MulticastBurstLength > this.bufferSize)
            {
                this.bufferSize = blockSize * response.MulticastBurstLength;
            }

            this.log.DebugFormat("UDP reader size: {0} bytes", this.bufferSize);
            await this.multicastClient.Connect(null, response.MulticastAddress, response.MulticastPort, this.Settings.Ttl);
            this.readTask = this.ReadTask(this.readCts.Token);
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

        internal async Task<ICollection<T>> ReceiveMulticast(TimeSpan readDelay)
        {
            this.jobAddedEvent.WaitOne(readDelay);
            int read = 0;
            int count = this.pendingDecodes.Count;
            List<T> ret = new List<T>(count);
            DecodeJob decodeJob;
            while (read++ < count && this.pendingDecodes.TryDequeue(out decodeJob))
            {
                ret.Add(await decodeJob.Task);
                this.bufferUse -= decodeJob.BufferUse;
            }

            return ret;
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

                if (this.jobAddedEvent != null)
                {
                    this.jobAddedEvent.Dispose();
                }

                this.pendingDecodes = null;
            }
        }

        private async Task ReadTask(CancellationToken token)
        {
            using (CancellationTokenRegistration ctr = token.Register(async () => await this.multicastClient.Close()))
            {
                for (; ;)
                {
                    token.ThrowIfCancellationRequested();
                    byte[] read = await this.multicastClient.Receive();
                    if (read == null)
                    {
                        break;
                    }

                    if (this.bufferUse + read.Length > this.bufferSize * 8 / 10)
                    {
                        continue;
                    }

                    this.bufferUse += read.Length;
                    this.pendingDecodes.Enqueue(this.DecodeTask(read, token));
                    this.jobAddedEvent.Set();
                }
            }
        }

        private DecodeJob DecodeTask(byte[] data, CancellationToken token)
        {
            return new DecodeJob(data.Length, Task.Run(() =>
            {
                byte[] next;
                if (this.encoderFactory != null)
                {
                    IDecoder decoder = this.encoderFactory.CreateDecoder();
                    next = decoder.Decode(data);
                }
                else
                {
                    next = data;
                }

                using (MemoryStream ms = new MemoryStream(next))
                {
                    T val = Serializer.Deserialize<T>(ms);
                    return val;
                }
            }));
        }

        private class DecodeJob
        {
            internal DecodeJob(int bufferUse, Task<T> task)
            {
                this.BufferUse = bufferUse;
                this.Task = task;
            }

            internal int BufferUse
            {
                get;
                private set;
            }

            internal Task<T> Task
            {
                get;
                private set;
            }
        }
    }
}
