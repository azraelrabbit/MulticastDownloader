// <copyright file="PassphraseSecret.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.Text;
    using EnsureThat;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;

    /// <summary>
    /// Represent a passphrase-based secret.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.ISecret" />
    public class PassphraseSecret : ISecret
    {
        private readonly Encoding enc;
        private string passPhrase;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassphraseSecret"/> class.
        /// </summary>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="enc">The encoding.</param>
        public PassphraseSecret(string passPhrase, Encoding enc)
        {
            Ensure.That(passPhrase, "passPhrase").IsNotNullOrEmpty();
            Ensure.That(enc, "enc").IsNotNull();
            this.passPhrase = passPhrase;
            this.enc = enc;
        }

        /// <summary>
        /// Creates the cipher.
        /// </summary>
        /// <param name="desiredLength">Length of the desired.</param>
        /// <returns>
        /// A cipher with the desired block length.
        /// </returns>
        public ICipherParameters CreateCipher(int desiredLength)
        {
            byte[] encoded = this.enc.GetBytes(this.passPhrase);
            if (encoded.Length < desiredLength)
            {
                byte[] ret = new byte[desiredLength];
                Buffer.BlockCopy(encoded, 0, ret, 0, encoded.Length);
                return new KeyParameter(ret);
            }

            return new KeyParameter(encoded);
        }
    }
}
