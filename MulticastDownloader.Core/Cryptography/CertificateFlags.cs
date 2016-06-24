// <copyright file="CertificateFlags.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;

    /// <summary>
    /// Represent certificate validation flags.
    /// </summary>
    [Flags]
    public enum CertificateFlags
    {
        /// <summary>
        /// No certificate flags
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Attempt to use the private key out of the certificate as the secret
        /// </summary>
        ReadPrivateKey = 0x1,

        /// <summary>
        /// Validate the certificate
        /// </summary>
        Validate = 0x2
    }
}
