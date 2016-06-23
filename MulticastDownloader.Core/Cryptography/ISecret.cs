// <copyright file="ISecret.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    /// <summary>
    /// Represent a cryptographic secret.
    /// </summary>
    public interface ISecret
    {
        /// <summary>
        /// Creates the key.
        /// </summary>
        /// <param name="desiredLength">Length of the desired.</param>
        /// <returns>A key.</returns>
        byte[] CreateKey(int desiredLength);
    }
}
