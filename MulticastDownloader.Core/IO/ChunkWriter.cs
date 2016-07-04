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
    internal class ChunkWriter : ChunkBase
    {
        private BitVector written;

        internal ChunkWriter(IEnumerable<FileChunk> chunks, BitVector written)
            : base(chunks)
        {
            Contract.Requires(written != null && written.Count == this.Count);
            this.written = written;
        }

        internal BitVector Written
        {
            get
            {
                return this.written;
            }
        }

        internal async Task WriteSegments(IEnumerable<FileSegment> segments)
        {
            Contract.Requires(segments != null);
            Task lastWrite = null;
            foreach (FileSegment segment in segments)
            {
                Contract.Requires(segment.SegmentId >= 0 && segment.SegmentId < this.Count);
                if (!this.written[segment.SegmentId])
                {
                    FileChunk chunk = this.Chunks[segment.SegmentId];
                    Contract.Assert(segment.Data.Length == chunk.Block.Length);
                    this.written[segment.SegmentId] = true;
                    if (lastWrite != null)
                    {
                        await lastWrite;
                    }

                    if (chunk.Stream.Position != chunk.Block.Offset)
                    {
                        await Task.Run(() => chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin));
                    }

                    lastWrite = chunk.Stream.WriteAsync(segment.Data, 0, segment.Data.Length);
                }
            }

            if (lastWrite != null)
            {
                await lastWrite;
            }
        }
    }
}
