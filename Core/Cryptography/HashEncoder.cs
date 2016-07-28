// <copyright file="HashEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Paddings;
    using Org.BouncyCastle.Crypto.Parameters;
    using Properties;

    internal class HashEncoder : IEncoder, IDecoder
    {
        private ICipherParameters cipherParameters;
        private BufferedBlockCipher cipher;

        internal HashEncoder(byte[] hash, int blockBits, bool encode)
        {
            if (blockBits % 8 != 0)
            {
                throw new ArgumentException(Resources.BlockSizeMustBeMultipleOf8);
            }

            this.cipherParameters = CreateCipher(hash, blockBits / 8);
            IBlockCipherPadding padding = new Pkcs7Padding();
            this.cipher = new PaddedBufferedBlockCipher(new RijndaelEngine(blockBits), padding);
            int keySize = blockBits >> 3;
            this.cipher.Init(encode, this.cipherParameters);
        }

        public byte[] Decode(byte[] encoded)
        {
            if (encoded == null)
            {
                throw new ArgumentNullException("encoded");
            }

            return this.cipher.Process(encoded);
        }

        public byte[] Encode(byte[] unencoded)
        {
            if (unencoded == null)
            {
                throw new ArgumentNullException("unencoded");
            }

            return this.cipher.Process(unencoded);
        }

        public int GetEncodedOutputLength(int input)
        {
            return this.cipher.GetOutputSize(input);
        }

        private static ICipherParameters CreateCipher(byte[] hash, int desiredLength)
        {
            if (hash.Length < desiredLength)
            {
                byte[] ret = new byte[desiredLength];
                Buffer.BlockCopy(hash, 0, ret, 0, hash.Length);
                return new KeyParameter(ret);
            }

            return new KeyParameter(hash);
        }
    }
}
