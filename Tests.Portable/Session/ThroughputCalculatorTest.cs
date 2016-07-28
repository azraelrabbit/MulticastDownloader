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
        [InlineData(1, new long[] { 1000 }, new int[] { 0 }, new long[] { 0 })]
        [InlineData(1, new long[] { 1000, 900, 800, 700 }, new int[] { 0, 1, 1, 1 }, new long[] { 0, 100, 100, 100 })]
        [InlineData(3, new long[] { 1000, 900, 800, 700 }, new int[] { 0, 1, 1, 1 }, new long[] { 0, 100, 100, 100 })]
        [InlineData(2, new long[] { 1000, 900, 800, 700 }, new int[] { 0, 1, 2, 1 }, new long[] { 0, 100, 75, 75 })]
        [InlineData(2, new long[] { 1000, 950, 800, 700 }, new int[] { 0, 1, 1, 1 }, new long[] { 0, 50, 100, 125 })]
        public void TestCanThroughputCalculatorCalculateThroughput(int maxIntervals, long[] bytesLeft, int[] timeIntervals, long[] avgBytesPerSecond)
        {
            ThroughputCalculator tc = new ThroughputCalculator(maxIntervals);
            DateTime start = DateTime.Now;
            tc.Start(bytesLeft[0], start);
            Assert.Equal(0, avgBytesPerSecond[0]);
            for (int i = 1; i < bytesLeft.Length; ++i)
            {
                start += TimeSpan.FromSeconds(timeIntervals[i]);
                long bytesPerSecond = tc.UpdateThroughput(bytesLeft[i], start);
                Assert.Equal(avgBytesPerSecond[i], bytesPerSecond);
            }
        }
    }
}
