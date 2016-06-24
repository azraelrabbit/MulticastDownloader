// <copyright file="ISecret.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using Org.BouncyCastle.Crypto;

    /// <summary>
    /// Represent a cryptographic secret.
    /// </summary>
    public interface ISecret
    {
        /// <summary>
        /// Creates the cipher.
        /// </summary>
        /// <param name="desiredLength">Length of the desired.</param>
        /// <returns>A cipher with the desired block length.</returns>
        ICipherParameters CreateCipher(int desiredLength);
    }
}
