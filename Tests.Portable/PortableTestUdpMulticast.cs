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

    internal class PortableTestUdpMulticast : IUdpMulticast
    {
        private static PortableTestUdpMulticastFactory factory = new PortableTestUdpMulticastFactory();
        private UdpSocket socket;
        private bool disposedValue = false;

        private PortableTestUdpMulticast()
        {
        }

        public static IUdpMulticastFactory Factory
        {
            get
            {
                return factory;
            }
        }

        public Task Connect(string interfaceName, string multicastAddr, int multicastPort, int ttl)
        {
            return Task.Run(() =>
            {
                string ifKey = (interfaceName ?? string.Empty) + ":" + (multicastAddr ?? string.Empty) + ":" + multicastPort + ":" + ttl;
                this.socket = factory.Connect(ifKey);
            });
        }

        public Task<byte[]> Receive()
        {
            return this.socket.Recieve();
        }

        public Task Send(byte[] data)
        {
            return this.socket.Send(data);
        }

        public Task Close()
        {
            return Task.Delay(0);
        }

        public void Dispose()
        {
            this.Dispose(true);
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

        private class UdpSocket : IDisposable
        {
            private object bufferLock = new object();
            private Queue<byte[]> buffer = new Queue<byte[]>();
            private AutoResetEvent sentEvent = new AutoResetEvent(false);
            private bool disposed = false;

            public void Dispose()
            {
                this.Dispose(true);
            }

            internal Task<byte[]> Recieve()
            {
                return Task.Run(() =>
                {
                    while (this.buffer.Count == 0)
                    {
                        this.sentEvent.WaitOne(200);
                    }

                    lock (this.bufferLock)
                    {
                        return this.buffer.Dequeue();
                    }
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

                    this.sentEvent.Set();
                });
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!this.disposed)
                {
                    this.disposed = true;
                    if (disposing)
                    {
                        if (this.sentEvent != null)
                        {
                            this.sentEvent.Dispose();
                        }
                    }
                }
            }
        }

        private class PortableTestUdpMulticastFactory : IUdpMulticastFactory
        {
            private object recieveLock = new object();
            private Dictionary<string, UdpSocket> socketsByIfKey = new Dictionary<string, UdpSocket>();

            public IUdpMulticast CreateMulticast()
            {
                return new PortableTestUdpMulticast();
            }

            internal UdpSocket Connect(string ifKey)
            {
                lock (this.recieveLock)
                {
                    if (!this.socketsByIfKey.ContainsKey(ifKey))
                    {
                        this.socketsByIfKey[ifKey] = new UdpSocket();
                    }

                    return this.socketsByIfKey[ifKey];
                }
            }
        }
    }
}
