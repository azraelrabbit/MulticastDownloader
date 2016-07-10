// <copyright file="IDecoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    /// <summary>
    /// Represent a decoder interface.
    /// </summary>
    public interface IDecoder
    {
        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        byte[] Decode(byte[] encoded);
    }
}
