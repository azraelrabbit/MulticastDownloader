// <copyright file="ICertificateValidator.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using Org.BouncyCastle.X509;

    /// <summary>
    /// Provides a means to check certificate authenticity for a session header.
    /// </summary>
    public interface ICertificateValidator
    {
        /// <summary>
        /// Checks the authenticity of the specified certificate.
        /// </summary>
        /// <param name="certificate">The DER-encoded certificate, which may be null.</param>
        /// <returns>True if the certificate is valid.</returns>
        bool CheckAuthenticity(byte[] certificate);
    }
}
