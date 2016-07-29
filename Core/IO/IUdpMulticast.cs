// <copyright file="IUdpMulticast.cs" company="MS">
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

    /// <summary>
    /// Represent the interface for a UDP multicast client.
    /// </summary>
    public interface IUdpMulticast : IDisposable
    {
        /// <summary>
        /// Connects to the specified multicast addr.
        /// </summary>
        /// <param name="interfaceName">The network interface name.</param>
        /// <param name="multicastAddr">The multicast address.</param>
        /// <param name="multicastPort">The multicast port.</param>
        /// <param name="ttl">The TTL.</param>
        /// <returns>A task object.</returns>
        Task Connect(string interfaceName, string multicastAddr, int multicastPort, int ttl);

        /// <summary>
        /// Reads the multicast data into the data received handler until the cancellation token is invoked.
        /// </summary>
        /// <param name="dataReceived">The data received.</param>
        /// <param name="token">The token.</param>
        /// <returns>A task object.</returns>
        Task Read(Action<byte[]> dataReceived, CancellationToken token);

        /// <summary>
        /// Sends the specified multicast data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>A task object.</returns>
        Task Send(byte[] data);

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <returns>A task object.</returns>
        Task Close();
    }
}
