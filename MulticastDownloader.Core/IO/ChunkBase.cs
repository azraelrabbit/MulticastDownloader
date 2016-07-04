// <copyright file="ChunkBase.cs" company="MS">
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

    internal class ChunkBase
    {
        private FileChunk[] chunks;
        private long numChunks;

        internal ChunkBase(IEnumerable<FileChunk> chunks)
        {
            Contract.Requires(chunks != null);
            this.chunks = chunks.ToArray();
            this.numChunks = chunks.LongCount();
        }

        internal FileChunk[] Chunks
        {
            get
            {
                return this.chunks;
            }
        }

        internal long Count
        {
            get
            {
                return this.numChunks;
            }
        }

        internal async Task Flush()
        {
            Task t = Task.Run(() =>
            {
                HashSet<Stream> toFlush = new HashSet<Stream>();
                foreach (FileChunk chunk in this.chunks)
                {
                    toFlush.Add(chunk.Stream);
                }

                toFlush.AsParallel().ForAll(async (s) =>
                {
                    await s.FlushAsync();
                });
            });

            await t;
        }
    }
}
