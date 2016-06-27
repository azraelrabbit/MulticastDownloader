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
    public class PassphraseEncoder : IEncoder
    {
        private ICipherParameters cipher;
        private BufferedBlockCipher encoder;
        private BufferedBlockCipher decoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassphraseEncoder"/> class.
        /// </summary>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="enc">The encoding.</param>
        /// <param name="blockBits">The desired encoding length in bits.</param>
        public PassphraseEncoder(string passPhrase, Encoding enc, int blockBits)
        {
            if (string.IsNullOrEmpty(passPhrase))
            {
                throw new ArgumentException(Resources.StringCannotBeNullOrEmpty, "passPhrase");
            }

            if (enc == null)
            {
                throw new ArgumentNullException("enc");
            }

            if (blockBits % 8 != 0)
            {
                throw new ArgumentException(Resources.BlockSizeMustBeMultipleOf8);
            }

            this.cipher = CreateCipher(passPhrase, enc, blockBits / 8);
            IBlockCipherPadding padding = new Pkcs7Padding();
            this.encoder = new PaddedBufferedBlockCipher(new RijndaelEngine(blockBits), padding);
            this.decoder = new PaddedBufferedBlockCipher(new RijndaelEngine(blockBits), padding);
            int keySize = blockBits >> 3;
            this.encoder.Init(true, this.cipher);
            this.decoder.Init(false, this.cipher);
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

        private static ICipherParameters CreateCipher(string passPhrase, Encoding encoding, int desiredLength)
        {
            byte[] encoded = encoding.GetBytes(passPhrase);
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
