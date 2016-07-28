// <copyright file="PassphraseEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.Text;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Paddings;
    using Org.BouncyCastle.Crypto.Parameters;
    using Properties;

    /// <summary>
    /// Represent a passphrase-based secret.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IEncoder" />
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IDecoder" />
    public class PassphraseEncoder : IEncoder, IDecoder
    {
        private HashEncoder encoder;

        internal PassphraseEncoder(string passPhrase, Encoding enc, int blockBits, bool encode)
        {
            if (string.IsNullOrEmpty(passPhrase))
            {
                throw new ArgumentException(Resources.StringCannotBeNullOrEmpty, "passPhrase");
            }

            if (enc == null)
            {
                throw new ArgumentNullException("enc");
            }

            byte[] hash = enc.GetBytes(passPhrase);
            this.encoder = new HashEncoder(hash, blockBits, encode);
        }

        /// <summary>
        /// Gets the length of the encoded output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The length in bytes.</returns>
        public int GetEncodedOutputLength(int input)
        {
            return this.encoder.GetEncodedOutputLength(input);
        }

        /// <summary>
        /// Encodes the specified unencoded.
        /// </summary>
        /// <param name="unencoded">The unencoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Encode(byte[] unencoded)
        {
            return this.encoder.Encode(unencoded);
        }

        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Decode(byte[] encoded)
        {
            return this.encoder.Decode(encoded);
        }
    }
}
