// <copyright file="FileChunk.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System.IO;

    internal class FileChunk
    {
        internal FileChunk(Stream stream, FileHeader header, FileBlockRange block)
        {
            this.Stream = stream;
            this.Header = header;
            this.Block = block;
        }

        internal Stream Stream { get; private set; }

        internal FileHeader Header { get; private set; }

        internal FileBlockRange Block { get; private set; }
    }
}
