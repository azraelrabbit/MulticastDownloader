// <copyright file="FileSetTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Session;
    using PCLStorage;
    using Xunit;
    using IO = System.IO;

    public class FileSetTest
    {
        [Theory]
        [InlineData("file1")]
        [InlineData("file1;file2")]
        [InlineData("file1;file2;dir1\file3")]
        public void FileTableConstructsWithFileList(string files)
        {
            string[] fileParts = files.Split(';');
            FileSet table = new FileSet(FileSystem.Current.LocalStorage, fileParts);
            Assert.Equal(0, table.NumSegments);
            Assert.Equal(fileParts.Length, table.FileHeaders.Count);
            int i = 0;
            foreach (FileHeader header in table.FileHeaders)
            {
                Assert.Equal(header.Name, fileParts[i]);
                ++i;
            }
        }

        [Theory]
        [InlineData(1500, @"foo", @"bar", new string[] { "foo", @"test2\bar", "baz" }, new long[] { 0, 649, 150000 })]
        [InlineData(1500, @"foo", @"bar", new string[] { "foo", @"test2\bar", "baz" }, new long[] { 150000, 150000, 150000 })]
        [InlineData(1000, @"foo", @"bif", new string[] { "foo", @"test2\bar", @"test3\bif", @"test3\foo", "baz" }, new long[] { 150000, 0, 149999, 0, 130000 })]
        public async Task FileTableReadsAndWritesWithAFolder(int blockSize, string inFolder, string outFolder, string[] fileNames, long[] fileSizes)
        {
            Assert.True(blockSize > 0);
            Assert.NotEqual(inFolder, outFolder);
            IFolder folder = await RecreateFolder(inFolder);
            Random r = new Random();
            long expectedNumSegments = 0;
            for (int i = 0; i < Math.Min(fileNames.Length, fileSizes.Length); ++i)
            {
                string fullPath = PortablePath.Combine(folder.Path, fileNames[i]);
                string dirName = IO.Path.GetDirectoryName(fullPath);
                await folder.CreateFolderAsync(dirName, CreationCollisionOption.OpenIfExists);
                IFile file = await folder.CreateFileAsync(fullPath, CreationCollisionOption.FailIfExists);
                using (IO.Stream s = await file.OpenAsync(FileAccess.ReadAndWrite))
                {
                    byte[] data = new byte[fileSizes[i]];
                    r.NextBytes(data);
                    s.Write(data, 0, data.Length);
                    expectedNumSegments += data.Length;
                }
            }

            List<FileChunk> originalChunks = null;
            ICollection<FileHeader> fileHeaders;
            expectedNumSegments /= blockSize;
            using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(inFolder), fileNames))
            {
                await table.InitRead(blockSize);
                ValidateBlocks(blockSize, fileNames, fileSizes, table);
                originalChunks = table.EnumerateChunks().ToList();
                fileHeaders = table.FileHeaders;
            }

            List<FileChunk> newChunks = null;
            folder = await RecreateFolder(outFolder);
            using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(outFolder), fileHeaders))
            {
                await table.InitWrite();
                ValidateBlocks(blockSize, fileNames, fileSizes, table);
                newChunks = table.EnumerateChunks().ToList();
            }

            CompareChunks(originalChunks, newChunks);
        }

        private static void ValidateBlocks(int blockSize, string[] fileNames, long[] fileSizes, FileSet table)
        {
            int i = 0;
            long k = 0;
            long setSize = 0;
            foreach (FileHeader header in table.FileHeaders)
            {
                Assert.NotNull(header.Blocks);
                Assert.Equal(fileNames[i], header.Name);
                Assert.Equal(fileSizes[i], header.Length);
                long totalLength = 0;
                foreach (FileBlockRange block in header.Blocks)
                {
                    Assert.Equal(k, block.SegmentId);
                    Assert.Equal(totalLength, block.Offset);
                    Assert.InRange(block.Length, 1, blockSize);
                    totalLength += block.Length;
                    ++k;
                }

                Assert.Equal(totalLength, fileSizes[i]);
                setSize += totalLength;
                ++i;
            }

            Assert.Equal(table.NumSegments, k);
        }

        private static void CompareChunks(List<FileChunk> originalChunks, List<FileChunk> newChunks)
        {
            Assert.Equal(originalChunks.Count, newChunks.Count);
            for (int i = 0; i < originalChunks.Count; ++i)
            {
                Assert.Equal(originalChunks[i].Header, newChunks[i].Header);
                Assert.Equal(originalChunks[i].Block, newChunks[i].Block);
                Assert.NotNull(originalChunks[i].Stream);
                Assert.NotNull(newChunks[i].Stream);
            }
        }

        private static async Task<IFolder> RecreateFolder(string rootFolder)
        {
            IFolder folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(rootFolder, CreationCollisionOption.ReplaceExisting);
            await folder.DeleteAsync();
            folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(rootFolder, CreationCollisionOption.ReplaceExisting);
            return folder;
        }
    }
}
