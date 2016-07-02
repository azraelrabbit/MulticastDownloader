// <copyright file="ChunkTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.IO;
    using Core.Server.IO;
    using Core.Session;
    using PCLStorage;
    using Xunit;
    using IO = System.IO;

    public class ChunkTest
    {
        [Theory]
        [InlineData(new long[] { 1, 2, 3 }, new long[] { 5, 5, 5, 10, 5, 5 })]
        public void ChunkReaderConstructs(long[] blocksPerHeader, long[] blockSizes)
        {
            IEnumerable<FileHeader> headers = this.BuildHeaders(blocksPerHeader, blockSizes);
            IEnumerable<FileChunk> chunks = this.BuildChunks(headers, false);
            ChunkReader reader = new ChunkReader(chunks);
            Assert.Equal(reader.NumChunks, chunks.LongCount());
        }

        [Theory]
        [InlineData(new long[] { 1, 2, 3 }, new long[] { 5, 5, 5, 10, 5, 5 })]
        public void ChunkWriterConstructs(long[] blocksPerHeader, long[] blockSizes)
        {
            IEnumerable<FileHeader> headers = this.BuildHeaders(blocksPerHeader, blockSizes);
            IEnumerable<FileChunk> chunks = this.BuildChunks(headers, true);
            ChunkWriter writer = new ChunkWriter(chunks);
        }

        [Theory]
        [InlineData(20, new long[] { 1, 2, 3 }, new long[] { 5, 5, 5, 10, 5, 5 })]
        [InlineData(20, new long[] { 2, 1, 3 }, new long[] { 5, 5, 0, 10, 5, 5 })]
        public async Task ChunkReaderReads(int numAttempts, long[] blocksPerHeader, long[] blockSizes)
        {
            IEnumerable<FileHeader> headers = this.BuildHeaders(blocksPerHeader, blockSizes);
            IList<FileChunk> myChunks = this.BuildChunks(headers, false);
            ChunkReader reader = new ChunkReader(myChunks);
            Assert.Equal(reader.NumChunks, myChunks.LongCount());
            Random r = new Random();
            for (int i = 0; i < numAttempts; ++i)
            {
                int id = r.Next((int)reader.NumChunks);
                FileSegment seg = await reader.ReadChunk(id);
                Assert.Equal(id, seg.SegmentId);
                for (int j = 0; j < seg.Data.Length; ++j)
                {
                    Assert.Equal(seg.Data[j], (byte)((myChunks[id].Block.Offset + j) & 0xFF));
                }
            }
        }

        [Theory]
        [InlineData(20, new long[] { 1, 2, 3 }, new long[] { 5, 5, 5, 10, 5, 5 })]
        [InlineData(20, new long[] { 2, 1, 3 }, new long[] { 5, 5, 0, 10, 5, 5 })]
        public async Task ChunkWriterWrites(int numAttempts, long[] blocksPerHeader, long[] blockSizes)
        {
            IEnumerable<FileHeader> headers = this.BuildHeaders(blocksPerHeader, blockSizes);
            IList<FileChunk> myChunks = this.BuildChunks(headers, true);
            ChunkWriter writer = new ChunkWriter(myChunks);
            Random r = new Random();
            for (int i = 0; i < numAttempts; ++i)
            {
                int id = r.Next(myChunks.Count);
                FileSegment seg = new FileSegment();
                seg.SegmentId = id;
                seg.Data = new byte[myChunks[id].Block.Length];
                long origLength = myChunks[id].Stream.Length;
                r.NextBytes(seg.Data);
                await writer.WriteChunk(seg);
                myChunks[id].Stream.Seek(myChunks[id].Block.Offset, IO.SeekOrigin.Begin);
                byte[] buf = new byte[myChunks[id].Block.Length];
                await myChunks[id].Stream.ReadAsync(buf, 0, buf.Length);
                Assert.True(seg.Data.SequenceEqual(buf));
                Assert.InRange(myChunks[id].Stream.Length, 0, myChunks[id].Header.Length);
            }
        }

        private IEnumerable<FileHeader> BuildHeaders(long[] blocksPerHeader, long[] blockSizes)
        {
            int i = 0;
            int k = 0;
            foreach (long numBlocks in blocksPerHeader)
            {
                FileHeader header = new FileHeader();
                header.Name = "file" + i++;
                List<FileBlockRange> blocks = new List<FileBlockRange>();
                long offset = 0;
                for (int j = 0; j < numBlocks; ++j)
                {
                    FileBlockRange blockRange = new FileBlockRange();
                    blockRange.SegmentId = k;
                    blockRange.Offset = offset;
                    blockRange.Length = blockSizes[k];
                    offset += blockRange.Length;
                    ++k;
                    blocks.Add(blockRange);
                }

                header.Blocks = blocks.ToArray();
                yield return header;
            }
        }

        private IList<FileChunk> BuildChunks(IEnumerable<FileHeader> headers, bool openWrite)
        {
            List<FileChunk> ret = new List<FileChunk>();
            foreach (FileHeader header in headers)
            {
                IO.Stream s;
                if (openWrite)
                {
                    s = new IO.MemoryStream((int)header.Length);
                }
                else
                {
                    byte[] b = new byte[header.Length];
                    for (int i = 0; i < b.Length; ++i)
                    {
                        b[i] = (byte)(i & 0xFF);
                    }

                    s = new IO.MemoryStream(b);
                }

                foreach (FileBlockRange blockRange in header.Blocks)
                {
                    ret.Add(new FileChunk(s, header, blockRange));
                }
            }

            return ret;
        }
    }
}
