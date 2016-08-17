// <copyright file="ChunkReader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.IO;
    using Session;

    // A asynchronous file chunk reader
    internal class ChunkReader : ChunkBase
    {
        private long startSeg;
        private long endSeg;
        private long segmentId;
        private BitVector read;

        internal ChunkReader(IEnumerable<FileChunk> chunks, BitVector readVector)
            : base(chunks)
        {
            Contract.Requires(readVector != null && readVector.Count == this.Count);
            this.startSeg = 0;
            this.endSeg = chunks.LongCount();
            this.segmentId = this.startSeg;
            this.read = readVector;
        }

        internal BitVector Read
        {
            get
            {
                return this.read;
            }
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
                    if (!this.read[chunk.Block.SegmentId])
                    {
                        bytes += chunk.Block.Length;
                    }
                }

                return bytes;
            }
        }

        internal async Task<IEnumerable<FileSegment>> ReadSegments(int numSegs)
        {
            Contract.Requires(numSegs >= 0);
            List<FileSegment> ret = new List<FileSegment>(numSegs);
            while (ret.Count < numSegs && this.segmentId < this.Count)
            {
                Contract.Requires(this.segmentId >= 0 && this.segmentId < this.Count);
                if (this.segmentId >= this.endSeg)
                {
                    this.segmentId = this.startSeg;
                    break;
                }

                if (!this.read[this.segmentId])
                {
                    FileChunk chunk = this.Chunks[this.segmentId];
                    FileSegment segment = new FileSegment();
                    segment.SegmentId = this.segmentId;
                    segment.Data = new byte[chunk.Block.Length];
                    if (chunk.Block.Offset != chunk.Stream.Position)
                    {
                        chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin);
                    }

                    await chunk.Stream.ReadAsync(segment.Data, 0, segment.Data.Length);
                    ret.Add(segment);
                }

                ++this.segmentId;
            }

            return ret;
        }

        internal void Reset()
        {
            this.segmentId = 0;
        }
    }
}
