// <copyright file="FileChunk.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.IO;

    internal class FileChunk : IDisposable
    {
        private bool disposedValue = false;

        internal FileChunk(Stream stream, FileHeader header, FileBlockRange block)
        {
            this.Stream = stream;
            this.Header = header;
            this.Block = block;
        }

        internal Stream Stream { get; private set; }

        internal FileHeader Header { get; private set; }

        internal FileBlockRange Block { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    if (this.Stream != null)
                    {
                        this.Stream.Dispose();
                        this.Stream = null;
                    }
                }

                this.disposedValue = true;
            }
        }
    }
}
