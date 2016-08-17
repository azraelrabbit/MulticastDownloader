// <copyright file="PortableTestUdpMulticast.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.IO;
    using Xunit;

    internal class PortableTestUdpMulticast : IUdpMulticast
    {
        private Random r = new Random();
        private PortableTestUdpMulticastFactory factory;
        private UdpSocket socket;
        private bool disposedValue = false;

        private PortableTestUdpMulticast(PortableTestUdpMulticastFactory factory)
        {
            this.factory = factory;
        }

        public Task Connect(string interfaceName, string multicastAddr, int multicastPort, int ttl)
        {
            return Task.Run(() =>
            {
                this.socket = this.factory.Connect(interfaceName, multicastAddr, multicastPort);
            });
        }

        public async Task<byte[]> Receive()
        {
            byte[] ret = await this.socket.Recieve();
            Assert.True(ret == null || ret.Length <= this.factory.Mtu);
            return ret;
        }

        public async Task Send(byte[] data)
        {
            Assert.True(data.Length <= this.factory.Mtu);
            if (this.r.NextDouble() <= this.factory.PacketReceptionRate)
            {
                await this.socket.Send(data);
            }
        }

        public Task Close()
        {
            return Task.Run(() => this.socket.Closed = true);
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        internal static IUdpMulticastFactory CreateFactory(int mtu, double packetReceptionRate)
        {
            return new PortableTestUdpMulticastFactory(mtu, packetReceptionRate);
        }

        internal static IUdpMulticastFactory CreateFactory(int mtu)
        {
            return CreateFactory(mtu, 1.0);
        }

        internal static IUdpMulticastFactory CreateFactory()
        {
            return CreateFactory(576, 1.0);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                this.disposedValue = true;
            }
        }

        private class UdpSocket
        {
            private object bufferLock = new object();
            private Queue<byte[]> buffer = new Queue<byte[]>();

            internal bool Closed
            {
                get;
                set;
            }

            internal Task<byte[]> Recieve()
            {
                return Task.Run(async () =>
                {
                    while (this.buffer.Count == 0 && !this.Closed)
                    {
                        await Task.Delay(200);
                    }

                    if (!this.Closed)
                    {
                        lock (this.bufferLock)
                        {
                            return this.buffer.Dequeue();
                        }
                    }

                    return null;
                });
            }

            internal Task Send(byte[] data)
            {
                return Task.Run(() =>
                {
                    lock (this.bufferLock)
                    {
                        this.buffer.Enqueue(data);
                    }
                });
            }
        }

        private class PortableTestUdpMulticastFactory : IUdpMulticastFactory
        {
            private readonly int mtu;
            private readonly double packetReceptionRate;
            private object socketsLock = new object();
            private Dictionary<string, UdpSocket> socketsByKey = new Dictionary<string, UdpSocket>();

            internal PortableTestUdpMulticastFactory(int mtu, double packetReceptionRate)
            {
                this.mtu = mtu;
                this.packetReceptionRate = packetReceptionRate;
            }

            internal int Mtu
            {
                get
                {
                    return this.mtu;
                }
            }

            internal double PacketReceptionRate
            {
                get
                {
                    return this.packetReceptionRate;
                }
            }

            public IUdpMulticast CreateMulticast()
            {
                return new PortableTestUdpMulticast(this);
            }

            internal UdpSocket Connect(string interfaceName, string multicastAddr, int multicastPort)
            {
                lock (this.socketsLock)
                {
                    string key = interfaceName + ":" + multicastAddr + ":" + multicastPort.ToString();
                    if (!this.socketsByKey.ContainsKey(key))
                    {
                        this.socketsByKey[key] = new UdpSocket();
                    }

                    return this.socketsByKey[key];
                }
            }
        }
    }
}
