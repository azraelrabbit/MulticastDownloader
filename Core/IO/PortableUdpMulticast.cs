// <copyright file="PortableUdpMulticast.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Properties;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    /// <summary>
    /// A portable UDP multicast implementation.
    /// </summary>
    /// <seealso cref="IUdpMulticast" />
    public class PortableUdpMulticast : IUdpMulticast
    {
        private ILog log = LogManager.GetLogger<PortableUdpMulticast>();
        private UdpSocketMulticastClient multicastClient = new UdpSocketMulticastClient();
        private TaskCompletionSource<byte[]> readTcs = new TaskCompletionSource<byte[]>();
        private bool disposed;

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns>
        /// A task object.
        /// </returns>
        public async Task Close()
        {
            TaskCompletionSource<byte[]> readTcs = this.readTcs;
            if (readTcs != null)
            {
                readTcs.TrySetCanceled();
            }

            if (this.multicastClient != null)
            {
                await this.multicastClient.DisconnectAsync();
            }
        }

        /// <summary>
        /// Connects to the specified multicast addr.
        /// </summary>
        /// <param name="interfaceName">The network interface name.</param>
        /// <param name="multicastAddr">The multicast address.</param>
        /// <param name="multicastPort">The multicast port.</param>
        /// <param name="ttl">The TTL.</param>
        /// <returns>
        /// A task object.
        /// </returns>
        public async Task Connect(string interfaceName, string multicastAddr, int multicastPort, int ttl)
        {
            this.multicastClient.TTL = ttl;
            ICommsInterface commsInterface = null;
            if (!string.IsNullOrEmpty(interfaceName))
            {
                foreach (ICommsInterface ci in await CommsInterface.GetAllInterfacesAsync())
                {
                    if (commsInterface.Name == interfaceName)
                    {
                        commsInterface = ci;
                        break;
                    }
                }

                if (commsInterface == null)
                {
                    throw new ArgumentException(Resources.InvalidInterfaceName);
                }
            }

            await this.multicastClient.JoinMulticastGroupAsync(multicastAddr, multicastPort, commsInterface);
            this.multicastClient.MessageReceived += (o, e) =>
            {
                TaskCompletionSource<byte[]> readTcs = this.readTcs;
                if (readTcs != null)
                {
                    readTcs.TrySetResult(e.ByteData);
                }
            };
        }

        /// <summary>
        /// Reads the next multicast data into the buffer.
        /// </summary>
        /// <returns>
        /// A task object which yields the next multicast data.
        /// </returns>
        public async Task<byte[]> Receive()
        {
            byte[] ret = await this.readTcs.Task;
            this.readTcs = new TaskCompletionSource<byte[]>();
            return ret;
        }

        /// <summary>
        /// Sends the specified multicast data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// A task object.
        /// </returns>
        public Task Send(byte[] data)
        {
            return this.multicastClient.SendMulticastAsync(data);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
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
