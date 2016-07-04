// <copyright file="IEncoderFactory.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    /// <summary>
    /// Represent an encoder factory.
    /// </summary>
    /// <remarks>
    /// For concrete examples of <see cref="IEncoderFactory"/>, see <see cref="AsymmetricEncoderFactory"/> or <see cref="PassphraseEncoderFactory"/>.
    /// </remarks>
    public interface IEncoderFactory
    {
        /// <summary>
        /// Creates the encoder.
        /// </summary>
        /// <returns>An encoder.</returns>
        IEncoder CreateEncoder();
    }
}
