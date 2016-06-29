// <copyright file="FileHeaderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Threading.Tasks;
    using Core.Session;
    using PCLStorage;
    using Xunit;
    using IO = System.IO;

    // Test FileHeader and FBR
    public class FileHeaderTest
    {
        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(3, 2, 1)]
        [InlineData(1, 4, 1)]
        public void FileBlockRangeInitializesWithValues(long offset, long length, long segmentId)
        {

        }
    }
}