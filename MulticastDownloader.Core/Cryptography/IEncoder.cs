// <copyright file="IEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using Org.BouncyCastle.Crypto;

    /// <summary>
    /// Represent a cryptographic secret.
    /// </summary>
    /// <remarks>The encoder is used to encrypt file data as it is being transmitted, as well as to authorize clients to receive files in a multicast session.</remarks>
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
    }
}
