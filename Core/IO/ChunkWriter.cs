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

        internal long TotalBytes
        {
            get
            {
                long bytes = 0;
                foreach (FileChunk chunk in this.Chunks)
                {
                    bytes += chunk.Block.Length;
                }

                return bytes;
            }
        }

        internal long BytesRemaining
        {
            get
            {
                long bytes = 0;
                foreach (FileChunk chunk in this.Chunks)
                {
                    if (!this.written[chunk.Block.SegmentId])
                    {
                        bytes += chunk.Block.Length;
                    }
                }

                return bytes;
            }
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
            foreach (FileSegment segment in segments)
            {
                Contract.Requires(segment.SegmentId >= 0 && segment.SegmentId < this.Count);
                if (!this.written[segment.SegmentId])
                {
                    this.written[segment.SegmentId] = true;
                    FileChunk chunk = this.Chunks[segment.SegmentId];
                    Contract.Assert(segment.Data.Length == chunk.Block.Length);
                    if (chunk.Stream.Position != chunk.Block.Offset)
                    {
                        chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin);
                    }

                    await chunk.Stream.WriteAsync(segment.Data, 0, segment.Data.Length);
                }
            }
        }
    }
}
