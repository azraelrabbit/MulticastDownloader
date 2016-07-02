// <copyright file="ChunkWriter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Session;

    // An asynchronous file chunk writer.
    internal class ChunkWriter
    {
        private FileChunk[] chunks;
        private long numChunks;

        internal ChunkWriter(IEnumerable<FileChunk> chunks)
        {
            this.chunks = chunks.ToArray();
            this.numChunks = chunks.LongCount();
        }

        internal async Task WriteChunk(FileSegment segment)
        {
            Contract.Requires(segment.SegmentId >= 0 && segment.SegmentId < this.numChunks);
            FileChunk chunk = this.chunks[segment.SegmentId];
            Contract.Assert(segment.Data.Length == chunk.Block.Length);
            await Task.Run(() => chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin));
            await chunk.Stream.WriteAsync(segment.Data, 0, segment.Data.Length);
        }
    }
}
