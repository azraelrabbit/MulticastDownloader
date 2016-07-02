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

    internal class ChunkTest
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
                }

                header.Blocks = blocks.ToArray();
                yield return header;
            }
        }

        private IEnumerable<FileChunk> BuildChunks(IEnumerable<FileHeader> headers, bool openWrite)
        {
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
                    yield return new FileChunk(s, header, blockRange);
                }
            }
        }
    }
}
