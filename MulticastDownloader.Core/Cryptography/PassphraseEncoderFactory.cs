// <copyright file="PassphraseEncoderFactory.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.Text;
    using Properties;

    /// <summary>
    /// Represent a factory for creating <see cref="PassphraseEncoder"/> objects.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IEncoderFactory" />
    public class PassphraseEncoderFactory : IEncoderFactory
    {
        private string passPhrase;
        private Encoding encoding;
        private int blockBits;

        /// <summary>
        /// Initializes a new instance of the <see cref="PassphraseEncoderFactory"/> class.
        /// </summary>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="enc">The enc.</param>
        /// <param name="blockBits">The block bits.</param>
        /// <exception cref="System.ArgumentException">
        /// passPhrase
        /// or
        /// </exception>
        /// <exception cref="System.ArgumentNullException">enc</exception>
        public PassphraseEncoderFactory(string passPhrase, Encoding enc, int blockBits)
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

            this.passPhrase = passPhrase;
            this.encoding = enc;
            this.blockBits = blockBits;
        }

        /// <summary>
        /// Creates the encoder.
        /// </summary>
        /// <returns>
        /// An encoder.
        /// </returns>
        public IEncoder CreateEncoder()
        {
            return new PassphraseEncoder(this.passPhrase, this.encoding, this.blockBits);
        }
    }
}
