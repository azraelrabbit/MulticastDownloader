// <copyright file="Encoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using EnsureThat;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Paddings;

    /// <summary>
    /// Represent a RSA-based encoder/decoder.
    /// </summary>
    public class Encoder
    {
        private BufferedBlockCipher encoder;
        private BufferedBlockCipher decoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="Encoder"/> class.
        /// </summary>
        /// <param name="secret">The secret.</param>
        public Encoder(ISecret secret)
            : this(128, secret)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Encoder"/> class.
        /// </summary>
        /// <param name="blockBits">The block bits.</param>
        /// <param name="secret">The secret.</param>
        public Encoder(int blockBits, ISecret secret)
        {
            Ensure.That(blockBits % 8, "blockBits").Is(0);
            Ensure.That(secret, "secret").IsNotNull();
            IBlockCipherPadding padding = new Pkcs7Padding();
            this.encoder = new PaddedBufferedBlockCipher(new RijndaelEngine(blockBits), padding);
            this.decoder = new PaddedBufferedBlockCipher(new RijndaelEngine(blockBits), padding);
            int keySize = blockBits >> 3;
            ICipherParameters cipher = secret.CreateCipher(keySize);
            this.encoder.Init(true, cipher);
            this.decoder.Init(false, cipher);
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
        /// Gets the length of the decoded output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The length in bytes.</returns>
        public int GetDecodedOutputLength(int input)
        {
            return this.decoder.GetOutputSize(input);
        }

        /// <summary>
        /// Encodes the specified unencoded.
        /// </summary>
        /// <param name="unencoded">The unencoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Encode(byte[] unencoded)
        {
            Ensure.That(unencoded, "unencoded").IsNotNull();
            return this.Process(this.encoder, unencoded);
        }

        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Decode(byte[] encoded)
        {
            Ensure.That(encoded, "encoded").IsNotNull();
            return this.Process(this.encoder, encoded);
        }

        private byte[] Process(BufferedBlockCipher cipher, byte[] input)
        {
            Ensure.That(input, "input").IsNotNull();
            cipher.Reset();

            int inputOffset = 0;
            int maximumOutputLength = cipher.GetOutputSize(input.Length);
            byte[] output = new byte[maximumOutputLength];
            int outputOffset = 0;
            int outputLength = 0;
            int bytesProcessed = cipher.ProcessBytes(input, inputOffset, input.Length, output, outputOffset);
            outputOffset += bytesProcessed;
            outputLength += bytesProcessed;
            bytesProcessed = cipher.DoFinal(output, outputOffset);
            outputOffset += bytesProcessed;
            outputLength += bytesProcessed;

            if (outputLength == output.Length)
            {
                return output;
            }
            else
            {
                byte[] truncatedOutput = new byte[outputLength];
                Buffer.BlockCopy(output, 0, truncatedOutput, 0, outputLength);
                return truncatedOutput;
            }
        }
    }
}
