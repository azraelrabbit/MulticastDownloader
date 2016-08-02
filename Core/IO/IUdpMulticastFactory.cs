// <copyright file="IUdpMulticastFactory.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    /// <summary>
    /// Represent an interface for creating <see cref="IUdpMulticast"/> objects.
    /// </summary>
    public interface IUdpMulticastFactory
    {
        /// <summary>
        /// Creates the multicast server.
        /// </summary>
        /// <returns>A multicast server.</returns>
        IUdpMulticast CreateMulticast();
    }
}
