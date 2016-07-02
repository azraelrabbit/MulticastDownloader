// <copyright file="ChunkReader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Session;

    // A asynchronous file chunk reader
    internal class ChunkReader
    {
        private FileChunk[] chunks;
        private long numChunks;

        internal ChunkReader(IEnumerable<FileChunk> chunks)
        {
            this.chunks = chunks.ToArray();
            this.numChunks = chunks.LongCount();
        }

        internal long NumChunks
        {
            get
            {
                return this.numChunks;
            }
        }

        internal async Task<FileSegment> ReadChunk(long segmentId)
        {
            Contract.Requires(segmentId >= 0 && segmentId < this.numChunks);
            FileChunk chunk = this.chunks[segmentId];
            await Task.Run(() => chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin));
            FileSegment segment = new FileSegment();
            segment.SegmentId = segmentId;
            segment.Data = new byte[chunk.Block.Length];
            await chunk.Stream.ReadAsync(segment.Data, 0, segment.Data.Length);
            return segment;
        }
    }
}
