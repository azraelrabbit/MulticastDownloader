// <copyright file="IMulticastSettings.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using Cryptography;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.X509;
    using PCLStorage;

    /// <summary>
    /// Represent multicast session settings.
    /// </summary>
    public interface IMulticastSettings
    {
        /// <summary>
        /// Gets the encoder used for encoding data and authorizing clients.
        /// </summary>
        /// <remarks>
        /// Encoding data can be used to authorize clients to receive data as well as to guarantee data can't be viewed by other users on your network, however
        /// encoding data can decrease transfer rates. This value can be null if you don't want to use encoded data. See the <see cref="Cryptography"/> namespace
        /// for built-in encoders.
        /// </remarks>
        /// <value>
        /// The encoder.
        /// </value>
        IEncoderFactory Encoder { get; }

        /// <summary>
        /// Gets the TTL (time to live) setting used for multicast data.
        /// </summary>
        /// <remarks>
        /// A value of 1 should be used if you only want to multicast to clients on your router.
        /// </remarks>
        /// <value>
        /// The TTL.
        /// </value>
        int Ttl { get; }

        /// <summary>
        /// Gets the size of the multicast buffer, in bytes, allocated to temporarily storing outbound or inbound multicast data.
        /// </summary>
        /// <value>
        /// The size of the multicast buffer.
        /// </value>
        int MulticastBufferSize { get; }

        /// <summary>
        /// Gets the TCP read timeout used for the session.
        /// </summary>
        /// <value>
        /// The read timeout.
        /// </value>
        TimeSpan ReadTimeout { get; }

        /// <summary>
        /// Gets the multicast root folder.
        /// </summary>
        /// <remarks>
        /// The root folder is the path under which the <see cref="UriParameters.Path"/> member refers for an individual session join request.
        /// </remarks>
        /// <value>
        /// The root folder.
        /// </value>
        IFolder RootFolder { get; }
    }
}
