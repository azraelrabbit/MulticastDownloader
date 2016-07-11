// <copyright file="IMulticastServerSettings.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.X509;
    using PCLStorage;

    /// <summary>
    /// Represent server settings for a multicast session.
    /// </summary>
    public interface IMulticastServerSettings
    {
        /// <summary>
        /// Gets the name of the interface being used to listen for session requests and send data.
        /// </summary>
        /// <remarks>
        /// A parameter of null can be used to indicate the client can listen for requests on any interface.
        /// However, this isn't recommended.
        /// </remarks>
        /// <value>
        /// The name of the interface.
        /// </value>
        string InterfaceName { get; }

        /// <summary>
        /// Gets the MTU (maximum transmission unit) for the network. Used for determining the size of multicast data.
        /// </summary>
        /// <remarks>
        /// The default MTU for internet connections is 576, however local connections should use a value
        /// of 1500 or greater. Check your network settings to determine what the optional MTU is for your network.
        /// </remarks>
        /// <value>
        /// The MTU.
        /// </value>
        int Mtu { get; }

        /// <summary>
        /// Gets the maximum number of pending connections.
        /// </summary>
        /// <value>
        /// The maximum number of pending connections.
        /// </value>
        int MaxConnectionsPerSession { get; }

        /// <summary>
        /// Gets the multicast address, which is used to broadcast messages from.
        /// </summary>
        /// <remarks>
        /// This must be a broadcast (IPV4) or multicast (IPV6) address. See RFC 919 or RFC 4291 for more details.
        /// </remarks>
        /// <value>
        /// The multicast address.
        /// </value>
        string MulticastAddress { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="MulticastAddress"/> parameter specifies an IPV6 address.
        /// </summary>
        /// <value>
        ///   <c>true</c> if ipv6; otherwise, <c>false</c>.
        /// </value>
        bool Ipv6 { get; }

        /// <summary>
        /// Gets the length of a multicast burst in bytes.
        /// </summary>
        /// <remarks>
        /// This value is largely system dependent. A burst length which is too large can lead to excessive packet loss during a multicast download.
        /// Ideally a burst length should be large enough that the interval between bursts does not cause clients to become starved for data.
        /// </remarks>
        /// <value>
        /// The length of the multicast burst.
        /// </value>
        int MulticastBurstLength { get; }

        /// <summary>
        /// Gets the multicast start port.
        /// </summary>
        /// <remarks>
        /// The multicast start port determines the starting port in the range of ports used to send data to individual multicast sessions.
        /// The <see cref="MaxSessions"/> parameter determines the port range.
        /// </remarks>
        /// <value>
        /// The multicast start port.
        /// </value>
        int MulticastStartPort { get; }

        /// <summary>
        /// Gets the maximum sessions.
        /// </summary>
        /// <remarks>
        /// As multicast sessions are created, they must reserve a multicast port. The number of ports which may be reserved is
        /// in the range [<see cref="MulticastStartPort"/>,<see cref="MulticastStartPort"/> + <see cref="MaxSessions"/>).
        /// </remarks>
        /// <value>
        /// The maximum sessions.
        /// </value>
        int MaxSessions { get; }
    }
}
