// <copyright file="IReceptionReporting.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    /// <summary>
    /// Represent an interface for reporting the packet reception rate.
    /// </summary>
    public interface IReceptionReporting
    {
        /// <summary>
        /// Gets the packet reception rate.
        /// </summary>
        /// <value>
        /// The reception rate, as a coefficient in the range [0,1].
        /// </value>
        double ReceptionRate { get; }
    }
}
