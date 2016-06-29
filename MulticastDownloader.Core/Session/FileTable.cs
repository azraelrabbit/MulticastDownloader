// <copyright file="FileTable.cs" company="MS">
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
    using PCLStorage;
    using Properties;

    // A steam-based file table
    internal class FileTable : IDisposable
    {
        private ILog log = LogManager.GetLogger<FileTable>();
        private bool disposed;
        private List<FileHeader> headers = new List<FileHeader>();
        private List<FileTableEntry> fileTableEntries = new List<FileTableEntry>();
        private IFolder rootFolder;
        private bool initialized;

        internal FileTable(IFolder rootFolder, IEnumerable<string> files)
        {
            Contract.Requires(rootFolder != null);
            Contract.Requires(files != null);
            this.rootFolder = rootFolder;
            foreach (string file in files)
            {
                FileHeader header = new FileHeader();
                header.Name = file;
            }

            this.CheckUnique(this.headers);
        }

        internal FileTable(IFolder rootFolder, IEnumerable<FileHeader> headers)
        {
            Contract.Requires(rootFolder != null);
            Contract.Requires(headers != null);
            this.rootFolder = rootFolder;
            this.headers = headers.ToList();
            this.CheckUnique(this.headers);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FileTable"/> class.
        /// </summary>
        ~FileTable()
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

        internal ICollection<FileTableEntry> FileTableEntries
        {
            get
            {
                return this.fileTableEntries;
            }
        }

        internal long NumSegments
        {
            get
            {
                if (this.fileTableEntries != null)
                {
                    long ret = 0;
                    foreach (FileTableEntry entry in this.fileTableEntries)
                    {
                        if (entry.Segments != null)
                        {
                            ret += entry.Segments.Count;
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

        internal async Task Clean()
        {
            foreach (FileHeader header in this.headers)
            {
                await this.rootFolder.Delete(header.Name);
            }
        }

        internal async Task InitWrite()
        {
            Contract.Requires(!this.initialized);
            Contract.Ensures(this.initialized);
            this.initialized = true;
            List<Task> pendingOperations = new List<Task>();
            foreach (FileHeader header in this.headers)
            {
                string curPath = Path.Combine(this.rootFolder.Path, header.Name);
                this.log.DebugFormat("File: opening {0}", curPath);
                string dirName = Path.GetDirectoryName(curPath);
                IFile file = await this.rootFolder.Create(header.Name, false);
                Stream s = await file.OpenAsync(FileAccess.ReadAndWrite);
                FileTableEntry fte = InitFteWrite(header, s);
                this.fileTableEntries.Add(fte);
                this.log.DebugFormat("{0} size: {1} bytes", header.Name, header.Length);
                pendingOperations.Add(file.Resize(header.Length));
            }

            foreach (Task pendingOperation in pendingOperations)
            {
                await pendingOperation;
            }
        }

        internal async Task InitRead(int blockSize)
        {
            Contract.Requires(blockSize >= 1000);
            Contract.Requires(!this.initialized);
            Contract.Ensures(this.initialized);
            this.initialized = true;
            SequenceGenerator g = new SequenceGenerator();
            List<Task> pendingOperations = new List<Task>();
            this.log.DebugFormat("Initializing with files under directory name: {0}", this.rootFolder.Path);
            foreach (FileHeader header in this.headers)
            {
                IFile file = await this.rootFolder.Create(header.Name, true);
                Stream s = await file.OpenAsync(FileAccess.Read);
                this.log.DebugFormat("{0} size: {1} bytes", header.Name, s.Length);
                this.InitFteRead(blockSize, header, s, g);
                pendingOperations.Add(Task.Run(async () => header.Checksum = await file.GetChecksum()));
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
                    foreach (FileTableEntry fte in this.fileTableEntries)
                    {
                        fte.Dispose();
                    }

                    this.fileTableEntries.Clear();
                }
            }
        }

        private static FileTableEntry InitFteWrite(FileHeader header, Stream s)
        {
            try
            {
                FileTableEntry fte = new FileTableEntry();
                fte.FileHeader = header;
                fte.FileStream = s;
                foreach (FileBlockRange block in header.Blocks)
                {
                    FileTableSegment segment = new FileTableSegment();
                    segment.Block = block;
                    segment.Entry = fte;
                    fte.Segments.Add(segment);
                }

                return fte;
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

        private void InitFteRead(int blockSize, FileHeader header, Stream s, SequenceGenerator g)
        {
            try
            {
                List<FileBlockRange> blocks = new List<FileBlockRange>();
                FileTableEntry fte = new FileTableEntry();
                fte.FileStream = s;
                fte.FileHeader = header;
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

                    FileTableSegment seg = new FileTableSegment();
                    seg.Entry = fte;
                    seg.Block = fbr;
                    blocks.Add(fbr);
                    fte.Segments.Add(seg);
                    remaining -= fbr.Length;
                }

                fte.FileHeader.Blocks = blocks.ToArray();
                this.fileTableEntries.Add(fte);
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

        internal class FileTableSegment
        {
            internal FileTableEntry Entry
            {
                get;
                set;
            }

            internal FileBlockRange Block
            {
                get;
                set;
            }
        }

        internal class FileTableEntry : IDisposable
        {
            private bool disposed;

            internal FileTableEntry()
            {
                this.Segments = new List<FileTableSegment>();
            }

            /// <summary>
            /// Finalizes an instance of the <see cref="FileTableEntry"/> class.
            /// </summary>
            ~FileTableEntry()
            {
                this.Dispose(false);
            }

            internal FileHeader FileHeader
            {
                get;
                set;
            }

            internal IList<FileTableSegment> Segments
            {
                get;
                private set;
            }

            internal Stream FileStream
            {
                get;
                set;
            }

            internal long SegmentStart
            {
                get
                {
                    FileTableSegment segment = this.Segments.FirstOrDefault();
                    if (segment != null)
                    {
                        return segment.Block.SegmentId;
                    }

                    return 0;
                }
            }

            internal long SegmentEnd
            {
                get
                {
                    FileTableSegment segment = this.Segments.LastOrDefault();
                    if (segment != null)
                    {
                        return segment.Block.SegmentId;
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
                        if (this.FileStream != null)
                        {
                            this.FileStream.Dispose();
                            this.FileStream = null;
                        }
                    }
                }
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
