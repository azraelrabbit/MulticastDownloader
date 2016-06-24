// <copyright file="CertificateSecret.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.X509;
    using Properties;

    /// <summary>
    /// Represent an X509 certificate based secret.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.ISecret" />
    public class CertificateSecret : ISecret
    {
        private X509Certificate cert;
        private CertificateFlags flags;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateSecret"/> class.
        /// </summary>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The cert flags.</param>
        public CertificateSecret(Stream certData, CertificateFlags flags)
        {
            X509CertificateParser parser = new X509CertificateParser();
            this.cert = parser.ReadCertificate(certData);
            this.flags = flags;
        }

        /// <summary>
        /// Creates the cipher.
        /// </summary>
        /// <param name="desiredLength">The desired length.</param>
        /// <returns>A cipher.</returns>
        /// <remarks>The desired length parameter is ignored.</remarks>
        public ICipherParameters CreateCipher(int desiredLength)
        {
            if (this.flags.HasFlag(CertificateFlags.Validate))
            {
                this.cert.CheckValidity();
            }

            AsymmetricKeyParameter kp = this.cert.GetPublicKey();
            if (this.flags.HasFlag(CertificateFlags.Validate))
            {
                if ((this.flags.HasFlag(CertificateFlags.ReadPrivateKey) && !kp.IsPrivate) || (!this.flags.HasFlag(CertificateFlags.ReadPrivateKey) && kp.IsPrivate))
                {
                    throw new InvalidOperationException(Resources.CertificateDoesNotContainPrivateKey);
                }
            }

            return kp;
        }
    }
}
