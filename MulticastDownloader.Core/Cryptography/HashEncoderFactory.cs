// <copyright file="HashEncoderFactory.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.Diagnostics.Contracts;

    internal class HashEncoderFactory : IEncoderFactory
    {
        private byte[] hash;
        private int blockBits;

        internal HashEncoderFactory(byte[] hash, int blockBits)
        {
            Contract.Requires(hash != null && (blockBits % 8) == 0);
            this.hash = hash;
            this.blockBits = blockBits;
        }

        public IEncoder CreateEncoder()
        {
            return new HashEncoder(this.hash, this.blockBits, true);
        }

        public IDecoder CreateDecoder()
        {
            return new HashEncoder(this.hash, this.blockBits, false);
        }
    }
}
