// <copyright file="ITransferReporting.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    /// <summary>
    /// Represent an interface for objects which report transfer status.
    /// </summary>
    public interface ITransferReporting
    {
        /// <summary>
        /// Gets the total bytes in the payload.
        /// </summary>
        /// <value>
        /// The total bytes in the payload.
        /// </value>
        long TotalBytes
        {
            get;
        }

        /// <summary>
        /// Gets the bytes remaining in the payload.
        /// </summary>
        /// <value>
        /// The bytes remaining.
        /// </value>
        long BytesRemaining
        {
            get;
        }

        /// <summary>
        /// Gets the bytes per second.
        /// </summary>
        /// <value>
        /// The bytes per second.
        /// </value>
        long BytesPerSecond
        {
            get;
        }
    }
}
