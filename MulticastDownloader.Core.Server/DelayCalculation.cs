// <copyright file="DelayCalculation.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    /// <summary>
    /// The delay calculation method.
    /// </summary>
    public enum DelayCalculation
    {
        /// <summary>
        /// Delay calculation by maximum throughput
        /// </summary>
        MaximumThroughput,

        /// <summary>
        /// Delay calculation by minimum throughput
        /// </summary>
        MinimumThroughput
    }
}
