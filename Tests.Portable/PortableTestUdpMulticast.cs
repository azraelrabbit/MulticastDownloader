// <copyright file="PortableTestUdpMulticast.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.IO;

    internal class PortableTestUdpMulticast : IUdpMulticast
    {
        private static object recieveLock = new object();
        private static Dictionary<string, BoxedTcs> pendingRecievesByIfNamePortAndAddr = new Dictionary<string, BoxedTcs>();
        private BoxedTcs pendingRecieve;
        private bool disposedValue = false;

        public Task Connect(string interfaceName, string multicastAddr, int multicastPort, int ttl)
        {
            return Task.Run(() =>
            {
                string ifKey = (interfaceName ?? string.Empty) + ":" + (multicastAddr ?? string.Empty) + ":" + multicastPort + ":" + ttl;
                lock (recieveLock)
                {
                    if (!pendingRecievesByIfNamePortAndAddr.ContainsKey(ifKey))
                    {
                        pendingRecievesByIfNamePortAndAddr[ifKey] = new BoxedTcs();
                    }

                    this.pendingRecieve = pendingRecievesByIfNamePortAndAddr[ifKey];
                }
            });
        }

        public Task<byte[]> Receive()
        {
            return this.pendingRecieve.GetRecieveTask();
        }

        public Task Send(byte[] data)
        {
            return this.pendingRecieve.SetRecieveTaskComplete(data);
        }

        public Task Close()
        {
            return Task.Run(() => { });
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

        private class BoxedTcs
        {
            private TaskCompletionSource<byte[]> recieveTcs = new TaskCompletionSource<byte[]>();

            internal async Task<byte[]> GetRecieveTask()
            {
                byte[] ret = await this.recieveTcs.Task;
                this.recieveTcs = new TaskCompletionSource<byte[]>();
                return ret;
            }

            internal Task SetRecieveTaskComplete(byte[] data)
            {
                return Task.Run(() =>
                {
                    this.recieveTcs.TrySetResult(data);
                });
            }
        }
    }
}
