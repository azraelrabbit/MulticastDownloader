// <copyright file="AsymmetricEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.OpenSsl;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent a key-based asymmetric encoder using PEM format certificates for storing key data.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IEncoder" />
    public class AsymmetricEncoder : IEncoder
    {
        private BufferedAsymmetricBlockCipher encoder;
        private BufferedAsymmetricBlockCipher decoder;

        internal AsymmetricEncoder(AsymmetricKeyParameter keyParam, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            this.Init(keyParam, flags, passwordFinder);
        }

        /// <summary>
        /// Gets the length of the encoded output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The length in bytes.</returns>
        public int GetEncodedOutputLength(int input)
        {
            return this.encoder.GetOutputSize(input);
        }

        /// <summary>
        /// Encodes the specified unencoded.
        /// </summary>
        /// <param name="unencoded">The unencoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Encode(byte[] unencoded)
        {
            if (unencoded == null)
            {
                throw new ArgumentNullException("unencoded");
            }

            return this.encoder.Process(unencoded);
        }

        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Decode(byte[] encoded)
        {
            if (encoded == null)
            {
                throw new ArgumentNullException("encoded");
            }

            return this.decoder.Process(encoded);
        }

        private void Init(AsymmetricKeyParameter keyParam, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            this.encoder = new BufferedAsymmetricBlockCipher(new RsaEngine());
            this.decoder = new BufferedAsymmetricBlockCipher(new RsaEngine());
            this.encoder.Init(true, keyParam);
            this.decoder.Init(false, keyParam);
        }
    }
}
