// <copyright file="ThroughputCalculator.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Linq;

    // A class which averages throughput measurements over time.
    internal class ThroughputCalculator
    {
        private uint nextInterval = 0;
        private uint sizeInterval = 0;
        private uint maxIntervals;
        private long[] bytesPerSecondAtInterval;
        private long bytesLeft;
        private DateTime lastUpdate = DateTime.MinValue;

        internal ThroughputCalculator(int maxIntervals)
        {
            Contract.Requires(maxIntervals > 0);
            this.maxIntervals = (uint)maxIntervals;
            this.bytesPerSecondAtInterval = new long[this.maxIntervals];
        }

        internal void Start(long totalBytes, DateTime when)
        {
            this.bytesLeft = totalBytes;
            this.lastUpdate = when;
            this.nextInterval = this.sizeInterval = 0;
            Array.Clear(this.bytesPerSecondAtInterval, 0, this.bytesPerSecondAtInterval.Length);
        }

        internal long UpdateThroughput(long bytesRemaining, DateTime when)
        {
            Contract.Requires(this.lastUpdate > DateTime.MinValue);
            this.bytesPerSecondAtInterval[this.nextInterval] = (long)((double)(bytesRemaining - this.bytesLeft) / (when - this.lastUpdate).TotalSeconds);
            this.nextInterval = (this.nextInterval + 1) % this.maxIntervals;
            this.bytesLeft = bytesRemaining;
            this.lastUpdate = when;
            if (this.sizeInterval < this.maxIntervals)
            {
                ++this.sizeInterval;
            }

            return this.bytesPerSecondAtInterval.Sum() / this.sizeInterval;
        }
    }
}
