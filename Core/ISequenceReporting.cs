// <copyright file="ISequenceReporting.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System.Collections.Generic;
    using Session;

    /// <summary>
    /// Represent an interface for reporting multicast sequence data.
    /// </summary>
    public interface ISequenceReporting
    {
        /// <summary>
        /// Gets the written bits.
        /// </summary>
        /// <value>
        /// The written bits.
        /// </value>
        BitVector Written
        {
            get;
        }

        /// <summary>
        /// Gets the current sequence number in the download.
        /// </summary>
        /// <value>
        /// The sequence number.
        /// </value>
        long SequenceNumber
        {
            get;
        }

        /// <summary>
        /// Gets the wave number for the session.
        /// </summary>
        /// <value>
        /// The wave number.
        /// </value>
        long WaveNumber
        {
            get;
        }
    }
}
