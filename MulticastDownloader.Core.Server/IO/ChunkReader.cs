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
    using Core.IO;
    using Core.Session;
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
            this.startSeg = chunks.First().Block.SegmentId;
            FileChunk last = chunks.Last();
            this.endSeg = last.Block.SegmentId + 1;
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

        internal async Task<IEnumerable<FileSegment>> ReadSegments(int numSegs)
        {
            Contract.Requires(numSegs >= 0);
            int remaining = numSegs;
            List<FileSegment> ret = new List<FileSegment>(numSegs);
            Task lastRead = null;
            while (remaining > 0)
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
                    --remaining;
                    ret.Add(segment);
                    if (lastRead != null)
                    {
                        await lastRead;
                    }

                    if (chunk.Block.Offset != chunk.Stream.Position)
                    {
                        await Task.Run(() => chunk.Stream.Seek(chunk.Block.Offset, SeekOrigin.Begin));
                    }

                    lastRead = chunk.Stream.ReadAsync(segment.Data, 0, segment.Data.Length);
                }

                ++this.segmentId;
            }

            if (lastRead != null)
            {
                await lastRead;
            }

            return ret;
        }
    }
}
