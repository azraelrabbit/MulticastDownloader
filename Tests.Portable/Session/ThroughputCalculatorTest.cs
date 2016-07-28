// <copyright file="ThroughputCalculatorTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Session;
    using PCLStorage;
    using Xunit;

    public class ThroughputCalculatorTest
    {
        [Theory]
        public void TestCanThroughputCalculatorCalculateThroughput(int maxIntervals, long[] bytesLeft, int[] timeIntervals, long[] avgBytesPerSecond)
        {
            ThroughputCalculator tc = new ThroughputCalculator(maxIntervals);
            DateTime start = DateTime.Now;
            tc.Start(bytesLeft[0], start);
            for (int i = 1; i < bytesLeft.Length; ++i)
            {
                start += TimeSpan.FromSeconds(timeIntervals[i]);
                long bytesPerSecond = tc.UpdateThroughput(bytesLeft[i], start);
                Assert.Equal(avgBytesPerSecond[i], bytesPerSecond);
            }
        }
    }
}
