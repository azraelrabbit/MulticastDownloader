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

        /// <summary>
        /// Initializes a new instance of the <see cref="PassphraseEncoderFactory"/> class.
        /// </summary>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="enc">The enc.</param>
        /// <exception cref="System.ArgumentException">
        /// passPhrase
        /// or
        /// </exception>
        /// <exception cref="System.ArgumentNullException">enc</exception>
        public PassphraseEncoderFactory(string passPhrase, Encoding enc)
        {
            if (string.IsNullOrEmpty(passPhrase))
            {
                throw new ArgumentException(Resources.StringCannotBeNullOrEmpty, "passPhrase");
            }

            if (enc == null)
            {
                throw new ArgumentNullException("enc");
            }

            this.passPhrase = passPhrase;
            this.encoding = enc;
        }

        /// <summary>
        /// Creates the encoder.
        /// </summary>
        /// <returns>
        /// An encoder.
        /// </returns>
        public IEncoder CreateEncoder()
        {
            return new PassphraseEncoder(this.passPhrase, this.encoding, 256, true);
        }

        /// <summary>
        /// Creates the decoder.
        /// </summary>
        /// <returns>
        /// A decoder.
        /// </returns>
        public IDecoder CreateDecoder()
        {
            return new PassphraseEncoder(this.passPhrase, this.encoding, 256, false);
        }
    }
}
