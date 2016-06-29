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
        private List<FileTableEntry> fileTableEntries = new List<FileTableEntry>();
        private long nextSeq;
        private bool initialized;
        private bool keepStreams;

        /// <summary>
        /// Finalizes an instance of the <see cref="FileTable"/> class.
        /// </summary>
        ~FileTable()
        {
            this.Dispose(false);
        }

        internal IList<FileTableEntry> FileTableEntries
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
                Contract.Requires(this.initialized);
                return this.nextSeq;
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

        internal void InitWrite(IDictionary<string, Stream> fileDataByNames, IEnumerable<FileHeader> files, bool ownsStreams)
        {
            Contract.Requires(fileDataByNames != null);
            Contract.Requires(files != null);
            Contract.Requires(!this.initialized);
            this.initialized = true;
            this.keepStreams = !ownsStreams;
            try
            {
                foreach (FileHeader header in files)
                {
                    FileTableEntry fte = new FileTableEntry();
                    fte.FileHeader = header;
                    fte.FileStream = fileDataByNames[header.Name];
                    foreach (FileBlockRange block in header.Blocks)
                    {
                        FileTableSegment segment = new FileTableSegment();
                        segment.Block = block;
                        segment.Entry = fte;
                        fte.Segments.Add(segment);
                        ++this.nextSeq;
                    }

                    this.fileTableEntries.Add(fte);
                    this.log.DebugFormat("{0} size: {1} bytes", header.Name, header.Length);
                    fte.FileStream.SetLength(fte.FileHeader.Length);
                    fte.FileStream.Position = 0;
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex);
                DisposeStreams(fileDataByNames);
                throw;
            }
        }

        internal async Task InitWrite(IFolder rootFolder, IEnumerable<FileHeader> files)
        {
            Contract.Requires(rootFolder != null);
            Contract.Requires(files != null);
            Contract.Requires(!this.initialized);
            Contract.Ensures(this.initialized);
            this.initialized = true;
            Dictionary<string, Stream> fileDataByNames = new Dictionary<string, Stream>(StringComparer.Ordinal);
            try
            {
                foreach (FileHeader header in files)
                {
                    string curPath = Path.Combine(rootFolder.Path, header.Name);
                    this.log.DebugFormat("File: opening {0}", curPath);
                    string dirName = Path.GetDirectoryName(curPath);
                    IFolder subfolder = await rootFolder.CreateFolderAsync(dirName, CreationCollisionOption.OpenIfExists);
                    IFile file = await subfolder.CreateFileAsync(Path.GetFileName(curPath), CreationCollisionOption.ReplaceExisting);
                    Stream s = await file.OpenAsync(FileAccess.ReadAndWrite);
                    fileDataByNames.Add(curPath, s);
                }

                this.InitWrite(fileDataByNames, files, true);
            }
            catch (Exception ex)
            {
                this.log.Error(ex);
                DisposeStreams(fileDataByNames);
                throw;
            }
        }

        internal void InitRead(int blockSize, IDictionary<string, Stream> fileDataByNames, bool ownsStreams)
        {
            Contract.Requires(blockSize > 0);
            Contract.Requires(fileDataByNames != null);
            this.initialized = true;
            this.keepStreams = !ownsStreams;
            try
            {
                foreach (KeyValuePair<string, Stream> kvp in fileDataByNames)
                {
                    this.log.DebugFormat("{0} size: {1} bytes", kvp.Key, kvp.Value.Length);
                    List<FileBlockRange> blocks = new List<FileBlockRange>();
                    FileTableEntry fte = new FileTableEntry();
                    fte.FileStream = kvp.Value;
                    fte.FileHeader = new FileHeader();
                    fte.FileHeader.Name = kvp.Key;
                    long remaining = kvp.Value.Length;
                    kvp.Value.Position = 0;
                    while (remaining > 0)
                    {
                        FileTableSegment seg = new FileTableSegment();
                        int segSize = blockSize;
                        if (remaining < blockSize)
                        {
                            segSize = (int)remaining;
                        }

                        seg.Entry = fte;
                        FileBlockRange fbr = new FileBlockRange();
                        fbr.SegmentId = this.nextSeq++;
                        fbr.Offset = kvp.Value.Length - remaining;
                        fbr.Length = segSize;
                        seg.Block = fbr;
                        blocks.Add(fbr);
                        fte.Segments.Add(seg);
                        remaining -= segSize;
                    }

                    fte.FileHeader.Blocks = blocks.ToArray();
                    this.fileTableEntries.Add(fte);
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex);
                DisposeStreams(fileDataByNames);
                throw;
            }
        }

        internal async Task InitRead(int blockSize, string fileOrDirectoryName)
        {
            Contract.Requires(!this.initialized);
            Contract.Ensures(this.initialized);
            this.initialized = true;
            this.log.DebugFormat("Initializing by file or directory name: {0}", fileOrDirectoryName);
            IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(fileOrDirectoryName);
            Dictionary<string, Stream> fileDataByNames = new Dictionary<string, Stream>(StringComparer.Ordinal);
            try
            {
                if (folder != null)
                {
                    await this.GetFilesRecursive(string.Empty, folder, fileDataByNames);
                }

                IFile file = await FileSystem.Current.GetFileFromPathAsync(fileOrDirectoryName);
                if (file != null)
                {
                    await this.GetFileData(string.Empty, file, fileDataByNames);
                }

                if (folder == null && file == null)
                {
                    throw new FileNotFoundException(Resources.FileNotFound, fileOrDirectoryName);
                }
            }
            catch (Exception ex)
            {
                this.log.Error(ex);
                DisposeStreams(fileDataByNames);
                throw;
            }

            this.InitRead(blockSize, fileDataByNames, true);
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
                    if (!this.keepStreams)
                    {
                        foreach (FileTableEntry fte in this.fileTableEntries)
                        {
                            fte.Dispose();
                        }
                    }

                    this.fileTableEntries.Clear();
                }
            }
        }

        private static void DisposeStreams(IDictionary<string, Stream> fileDataByNames)
        {
            foreach (KeyValuePair<string, Stream> kvp in fileDataByNames)
            {
                kvp.Value.Dispose();
            }
        }

        private async Task GetFilesRecursive(string path, IFolder folder, IDictionary<string, Stream> fileDataByNames)
        {
            string curPath = Path.Combine(path, folder.Name);
            foreach (IFile file in await folder.GetFilesAsync())
            {
                await this.GetFileData(curPath, file, fileDataByNames);
            }

            foreach (IFolder childFolder in await folder.GetFoldersAsync())
            {
                await this.GetFilesRecursive(curPath, childFolder, fileDataByNames);
            }
        }

        private async Task GetFileData(string path, IFile file, IDictionary<string, Stream> fileDataByNames)
        {
            string filePath = Path.Combine(path, file.Name);
            this.log.DebugFormat("File: {0}", filePath);
            Stream s = await file.OpenAsync(FileAccess.Read);
            if (fileDataByNames.ContainsKey(filePath))
            {
                throw new InvalidOperationException(Resources.DuplicateFile);
            }

            fileDataByNames[filePath] = s;
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
    }
}
