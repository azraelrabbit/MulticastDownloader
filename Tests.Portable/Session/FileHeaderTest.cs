// <copyright file="FileHeaderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Collections.Generic;
    using Core.Session;
    using Xunit;

    // Test FileHeader and FBR
    public class FileHeaderTest
    {
        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(3, 2, 1)]
        [InlineData(1, 4, 1)]
        public void FileBlockRangeInitializesWithValues(long offset, long length, long segmentId)
        {
            FileBlockRange fbr = new FileBlockRange();
            fbr.Offset = offset;
            fbr.Length = length;
            fbr.SegmentId = segmentId;
            Assert.Equal(offset, fbr.Offset);
            Assert.Equal(length, fbr.Length);
            Assert.Equal(segmentId, fbr.SegmentId);
        }

        [Theory]
        [InlineData(1, 2, 3)]
        [InlineData(3, 2, 1)]
        [InlineData(1, 4, 1)]
        public void FileBlockEqualsSelf(long offset, long length, long segmentId)
        {
            FileBlockRange fbr1 = new FileBlockRange();
            fbr1.Offset = offset;
            fbr1.Length = length;
            fbr1.SegmentId = segmentId;
            FileBlockRange fbr2 = new FileBlockRange();
            fbr2.Offset = offset;
            fbr2.Length = length;
            fbr2.SegmentId = segmentId;
            Assert.Equal(fbr1, fbr2);
            Assert.Equal(fbr1, fbr1);
            Assert.Equal(fbr1.GetHashCode(), fbr2.GetHashCode());
        }

        [Theory]
        [InlineData(1, 2, 3, 3, 2, 1)]
        [InlineData(3, 2, 1, 1, 4, 1)]
        [InlineData(1, 4, 1, 1, 2, 3)]
        public void FileBlockDoesNotEqualAnotherFileBlock(long o1, long l1, long s1, long o2, long l2, long s2)
        {
            FileBlockRange fbr1 = new FileBlockRange();
            fbr1.Offset = o1;
            fbr1.Length = l1;
            fbr1.SegmentId = s1;
            FileBlockRange fbr2 = new FileBlockRange();
            fbr2.Offset = o2;
            fbr2.Length = l2;
            fbr2.SegmentId = s2;
            Assert.NotEqual(fbr1, fbr2);
            Assert.Equal(fbr1, fbr1);
        }

        [Theory]
        [InlineData("foo", 123, new long[] { 1, 2, 3, 3, 2, 1 })]
        [InlineData("bar", 123, new long[] { 3, 2, 1 })]
        [InlineData("bar", 123, null)]
        public void FileHeaderEqualsSelf(string name, int checksum, long[] fbrArguments)
        {
            FileHeader f1 = BuildHeader(name, checksum, fbrArguments);
            FileHeader f2 = BuildHeader(name, checksum, fbrArguments);
            Assert.Equal(f1, f1);
            Assert.Equal(f1, f2);
            Assert.Equal(f1.GetHashCode(), f2.GetHashCode());
            long expectedLength = 0;
            if (fbrArguments != null)
            {
                for (int i = 0; i < fbrArguments.Length; i += 3)
                {
                    expectedLength += fbrArguments[i + 1];
                }
            }

            Assert.Equal(f1.Length, expectedLength);
        }

        [Theory]
        [InlineData("foo", 123, new long[] { 1, 2, 3, 3, 2, 1 }, "foo", 123, new long[] { 3, 2, 1 })]
        [InlineData("bar", 123, new long[] { 3, 2, 1 }, "bar", 456, new long[] { 3, 2, 1 })]
        [InlineData("bar", 123, null, "bar", 123, new long[] { 3, 2, 1 })]
        public void FileHeaderDoesNotEqualANotherFileHeader(string n1, int c1, long[] a1, string n2, int c2, long[] a2)
        {
            FileHeader f1 = BuildHeader(n1, c1, a1);
            FileHeader f2 = BuildHeader(n2, c2, a2);
            Assert.NotEqual(f1, f2);
        }

        private static FileHeader BuildHeader(string name, int checksum, long[] fbrArguments)
        {
            FileHeader header = new FileHeader();
            header.Name = name;
            header.Checksum = checksum;
            List<FileBlockRange> blocks = new List<FileBlockRange>();
            if (fbrArguments != null)
            {
                for (int i = 0; i < fbrArguments.Length; i += 3)
                {
                    long o = fbrArguments[i];
                    long l = fbrArguments[i + 1];
                    long s = fbrArguments[i + 2];
                    FileBlockRange fbr = new FileBlockRange();
                    fbr.Offset = o;
                    fbr.Length = l;
                    fbr.SegmentId = s;
                    blocks.Add(fbr);
                }

                header.Blocks = blocks.ToArray();
            }

            return header;
        }
    }
}