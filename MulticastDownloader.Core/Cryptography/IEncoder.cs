// <copyright file="IEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using Org.BouncyCastle.Crypto;

    /// <summary>
    /// Represent a cryptographic secret.
    /// </summary>
    public interface IEncoder
    {
        /// <summary>
        /// Gets the length of the encoded output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The length in bytes.</returns>
        int GetEncodedOutputLength(int input);

        /// <summary>
        /// Encodes the specified unencoded.
        /// </summary>
        /// <param name="unencoded">The unencoded data.</param>
        /// <returns>A byte array.</returns>
        byte[] Encode(byte[] unencoded);

        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        byte[] Decode(byte[] encoded);
    }
}
