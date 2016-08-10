// <copyright file="FileSet.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Logging;
    using IO;
    using PCLStorage;
    using Properties;

    // A steam-based file table
    internal class FileSet : IDisposable
    {
        private ILog log = LogManager.GetLogger<FileSet>();
        private bool disposed;
        private List<FileHeader> headers = new List<FileHeader>();
        private List<Stream> streamsByHeaderIndex = new List<Stream>();
        private IFolder rootFolder;

        internal FileSet(IFolder rootFolder, IEnumerable<string> files)
        {
            Contract.Requires(rootFolder != null);
            Contract.Requires(files != null);
            this.rootFolder = rootFolder;
            foreach (string file in files)
            {
                FileHeader header = new FileHeader();
                header.Name = file;
                this.headers.Add(header);
            }

            this.CheckUnique(this.headers);
        }

        internal FileSet(IFolder rootFolder, IEnumerable<FileHeader> headers)
        {
            Contract.Requires(rootFolder != null);
            Contract.Requires(headers != null);
            this.rootFolder = rootFolder;
            this.headers = headers.ToList();
            this.CheckUnique(this.headers);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FileSet"/> class.
        /// </summary>
        ~FileSet()
        {
            this.Dispose(false);
        }

        internal ICollection<FileHeader> FileHeaders
        {
            get
            {
                return this.headers;
            }
        }

        internal long NumSegments
        {
            get
            {
                if (this.headers != null)
                {
                    long ret = 0;
                    foreach (FileHeader header in this.headers)
                    {
                        if (header.Blocks != null)
                        {
                            ret += header.Blocks.Length;
                        }
                    }

                    return ret;
                }

                return 0;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal IEnumerable<FileChunk> EnumerateChunks()
        {
            int i = 0;
            foreach (FileHeader header in this.headers)
            {
                if (header.Blocks != null)
                {
                    foreach (FileBlockRange fbr in header.Blocks)
                    {
                        yield return new FileChunk(this.streamsByHeaderIndex[i], header, fbr);
                    }
                }

                ++i;
            }
        }

        internal async Task Clean()
        {
            foreach (Stream s in this.streamsByHeaderIndex)
            {
                s.Dispose();
            }

            this.streamsByHeaderIndex.Clear();

            foreach (FileHeader header in this.headers)
            {
                await this.rootFolder.Delete(header.Name);
            }
        }

        internal async Task InitWrite()
        {
            List<Task> pendingOperations = new List<Task>();
            FileAccess access = FileAccess.ReadAndWrite;
            foreach (FileHeader header in this.headers)
            {
                string curPath = Path.Combine(this.rootFolder.Path, header.Name);
                this.log.DebugFormat("File: opening {0}", curPath);
                string dirName = Path.GetDirectoryName(curPath);
                IFile file = await this.rootFolder.Create(header.Name, false);
                using (Stream s = await file.OpenAsync(access))
                {
                    this.log.DebugFormat("{0} size: {1} bytes", header.Name, header.Length);
                }

                pendingOperations.Add(file.Resize(header.Length));
            }

            foreach (Task pendingOperation in pendingOperations)
            {
                await pendingOperation;
            }

            foreach (FileHeader header in this.headers)
            {
                IFile file = await this.rootFolder.Create(header.Name, false);
                this.streamsByHeaderIndex.Add(await file.OpenAsync(access));
            }
        }

        internal async Task InitRead(int blockSize)
        {
            Contract.Requires(blockSize >= 576);
            SequenceGenerator g = new SequenceGenerator();
            List<Task> pendingOperations = new List<Task>();
            this.log.DebugFormat("Initializing with files under directory name: {0}", this.rootFolder.Path);
            FileAccess access = FileAccess.Read;
            foreach (FileHeader header in this.headers)
            {
                IFile file = await this.rootFolder.Create(header.Name, true);
                using (Stream s = await file.OpenAsync(access))
                {
                    this.log.DebugFormat("{0} size: {1} bytes", header.Name, s.Length);
                    this.InitFileRead(blockSize, header, s, g);
                }

                pendingOperations.Add(Task.Run(async () => header.Checksum = await file.GetChecksum()));
            }

            foreach (Task pendingOperation in pendingOperations)
            {
                await pendingOperation;
            }

            foreach (FileHeader header in this.headers)
            {
                IFile file = await this.rootFolder.Create(header.Name, false);
                this.streamsByHeaderIndex.Add(await file.OpenAsync(access));
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    foreach (Stream s in this.streamsByHeaderIndex)
                    {
                        s.Dispose();
                    }

                    this.streamsByHeaderIndex.Clear();
                }
            }
        }

        private void CheckUnique(IEnumerable<FileHeader> files)
        {
            HashSet<string> fileNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (FileHeader header in files)
            {
                if (fileNames.Contains(header.Name))
                {
                    throw new InvalidOperationException(Resources.DuplicateFile);
                }

                fileNames.Add(header.Name);
            }
        }

        private void InitFileRead(int blockSize, FileHeader header, Stream s, SequenceGenerator g)
        {
            try
            {
                List<FileBlockRange> blocks = new List<FileBlockRange>();
                long remaining = s.Length;
                s.Position = 0;
                while (remaining > 0)
                {
                    FileBlockRange fbr = new FileBlockRange();
                    fbr.SegmentId = g.GetNextSeq();
                    fbr.Offset = s.Length - remaining;
                    fbr.Length = blockSize - FileSegment.GetSegmentOverhead(fbr.SegmentId, blockSize);
                    if (remaining < fbr.Length)
                    {
                        fbr.Length = (int)remaining;
                    }

                    byte[] b = new byte[fbr.Length];
                    s.Seek(fbr.Offset, SeekOrigin.Begin);
                    s.Read(b, 0, (int)fbr.Length);
                    blocks.Add(fbr);
                    remaining -= fbr.Length;
                }

                header.Blocks = blocks.ToArray();
            }
            catch (Exception)
            {
                if (s != null)
                {
                    s.Dispose();
                }

                throw;
            }
        }

        private class SequenceGenerator
        {
            private long nextSeq = 0;

            internal long GetNextSeq()
            {
                return this.nextSeq++;
            }
        }
    }
}
