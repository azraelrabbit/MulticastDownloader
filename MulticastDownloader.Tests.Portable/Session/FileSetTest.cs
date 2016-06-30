﻿// <copyright file="FileTableTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core.Session;
    using PCLStorage;
    using Xunit;
    using IO = System.IO;

    //public class FileSetTest
    //{
    //    [Fact]
    //    public void FileTableConstructsWithFileList(string[] files)
    //    {
    //        FileSet table = new FileSet(FileSystem.Current.LocalStorage, files);
    //        Assert.Equal(0, table.NumSegments);
    //        Assert.Equal(0, table.FileTableEntries.Count);
    //    }

    //    [Theory]
    //    [InlineData(1500, @"foo", @"bar", new string[] { "foo", @"test2\bar", "baz" }, new long[] { 0, 649, 150000 })]
    //    [InlineData(1500, @"foo", @"bar", new string[] { "foo", @"test2\bar", "baz" }, new long[] { 150000, 150000, 150000 })]
    //    public async Task FileTableReadsAndWritesWithAFolder(int blockSize, string inFolder, string outFolder, string[] fileNames, long[] fileSizes)
    //    {
    //        Assert.True(blockSize > 0);
    //        Assert.NotEqual(inFolder, outFolder);
    //        IFolder folder = await RecreateFolder(inFolder);
    //        Random r = new Random();
    //        long expectedNumSegments = 0;
    //        for (int i = 0; i < Math.Min(fileNames.Length, fileSizes.Length); ++i)
    //        {
    //            string fullPath = PortablePath.Combine(folder.Path, fileNames[i]);
    //            string dirName = IO.Path.GetDirectoryName(fullPath);
    //            await folder.CreateFolderAsync(dirName, CreationCollisionOption.ReplaceExisting);
    //            IFile file = await folder.CreateFileAsync(fullPath, CreationCollisionOption.FailIfExists);
    //            using (IO.Stream s = await file.OpenAsync(FileAccess.ReadAndWrite))
    //            {
    //                byte[] data = new byte[fileSizes[i]];
    //                r.NextBytes(data);
    //                s.Write(data, 0, data.Length);
    //                expectedNumSegments += data.Length;
    //            }
    //        }

    //        ICollection<FileSet.FileTableEntry> tableEntries = null;
    //        expectedNumSegments /= blockSize;
    //        using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(inFolder), fileNames))
    //        {
    //            await table.InitRead(blockSize);
    //            CheckReadBlocks(blockSize, expectedNumSegments, fileNames, fileSizes, table);
    //            tableEntries = table.FileTableEntries;
    //        }

    //        List<FileHeader> files = new List<FileHeader>();
    //        foreach (FileSet.FileTableEntry fte in tableEntries)
    //        {
    //            files.Add(fte.FileHeader);
    //        }

    //        folder = await RecreateFolder(outFolder);
    //        using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(outFolder), fileNames))
    //        {
    //            await table.InitWrite();
    //            await CheckWriteBlocks(blockSize, expectedNumSegments, fileNames, fileSizes, folder, table);
    //        }
    //    }

    //    [Theory]
    //    [InlineData(1500, @"foo", @"bar", "foo", 0)]
    //    [InlineData(1500, @"foo", @"bar", "bar", 643)]
    //    [InlineData(1500, @"foo", @"bar", "baz", 150000)]
    //    public async Task FileTableReadsAndWritesWithAFile(int blockSize, string inFolder, string outFolder, string fileName, long fileSize)
    //    {
    //        Assert.True(blockSize > 0);
    //        Assert.NotEqual(inFolder, outFolder);
    //        IFolder folder = await RecreateFolder(inFolder);
    //        Random r = new Random();
    //        long expectedNumSegments = 0;
    //        string fullPath = PortablePath.Combine(folder.Path, fileName);
    //        string dirName = IO.Path.GetDirectoryName(fullPath);
    //        await folder.CreateFolderAsync(dirName, CreationCollisionOption.ReplaceExisting);
    //        IFile file = await folder.CreateFileAsync(fullPath, CreationCollisionOption.FailIfExists);
    //        using (IO.Stream s = await file.OpenAsync(FileAccess.ReadAndWrite))
    //        {
    //            byte[] data = new byte[fileSize];
    //            r.NextBytes(data);
    //            s.Write(data, 0, data.Length);
    //            expectedNumSegments += data.Length;
    //        }

    //        ICollection<FileSet.FileTableEntry> tableEntries = null;
    //        expectedNumSegments /= blockSize;
    //        using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(inFolder), new string[] { fileName }))
    //        {
    //            await table.InitRead(blockSize);
    //            CheckReadBlocks(blockSize, expectedNumSegments, new string[] { fileName }, new long[] { fileSize }, table);
    //            tableEntries = table.FileTableEntries;
    //        }

    //        List<FileHeader> files = new List<FileHeader>();
    //        foreach (FileSet.FileTableEntry fte in tableEntries)
    //        {
    //            files.Add(fte.FileHeader);
    //        }

    //        folder = await RecreateFolder(outFolder);
    //        using (FileSet table = new FileSet(await FileSystem.Current.LocalStorage.GetFolderAsync(outFolder), new string[] { fileName }))
    //        {
    //            await table.InitWrite();
    //            await CheckWriteBlocks(blockSize, expectedNumSegments, new string[] { fileName }, new long[] { fileSize }, folder, table);
    //        }
    //    }

    //    private static void CheckReadBlocks(int blockSize, long expectedNumSegments, string[] fileNames, long[] fileSizes, FileSet table)
    //    {
    //        Assert.Equal(expectedNumSegments, table.NumSegments);
    //        long totalSegments = 0;
    //        long k = 0;
    //        foreach (FileSet.FileTableEntry entry in table.FileTableEntries)
    //        {
    //            Assert.NotNull(entry.FileStream);
    //            Assert.Equal(entry.FileHeader.Blocks.Length, entry.Segments.Count);
    //            for (int j = 0; j < entry.Segments.Count; ++j)
    //            {
    //                Assert.Equal(entry.FileHeader.Blocks[j], entry.Segments[j].Block);
    //                FileSet.FileTableSegment segment = entry.Segments[j];
    //                Assert.Equal(segment.Entry, entry);
    //                Assert.Equal(k, segment.Block.SegmentId);
    //                Assert.Equal(k - segment.Entry.SegmentStart, segment.Block.Offset);
    //                Assert.True(segment.Block.Length <= blockSize);
    //                Assert.Equal(segment.Entry.SegmentEnd - k, segment.Entry.SegmentStart);
    //                totalSegments += segment.Block.Length;
    //                ++k;
    //            }
    //        }

    //        Assert.Equal(expectedNumSegments, totalSegments);
    //    }

    //    private static async Task CheckWriteBlocks(int blockSize, long expectedNumSegments, string[] fileNames, long[] fileSizes, IFolder folder, FileSet table)
    //    {
    //        long count = Math.Min(fileNames.Length, fileSizes.Length);
    //        long totalSegments = 0;
    //        long totalFileSizeSegments = 0;
    //        long k = 0;
    //        long i = 0;
    //        Assert.Equal(table.FileTableEntries.Count, count);
    //        foreach (FileSet.FileTableEntry entry in table.FileTableEntries)
    //        {
    //            IFile file = await folder.GetFileAsync(fileNames[i]);
    //            Assert.NotNull(file);
    //            Assert.NotNull(entry.FileStream);
    //            totalFileSizeSegments += entry.FileStream.Length;
    //            Assert.Equal(entry.FileHeader.Blocks.Length, entry.Segments.Count);
    //            for (int j = 0; j < entry.Segments.Count; ++j)
    //            {
    //                Assert.Equal(entry.FileHeader.Blocks[j], entry.Segments[j].Block);
    //                FileSet.FileTableSegment segment = entry.Segments[j];
    //                Assert.Equal(segment.Entry, entry);
    //                Assert.Equal(k, segment.Block.SegmentId);
    //                Assert.Equal(k - segment.Entry.SegmentStart, segment.Block.Offset);
    //                Assert.True(segment.Block.Length <= blockSize);
    //                Assert.Equal(segment.Entry.SegmentEnd - k, segment.Entry.SegmentStart);
    //                totalSegments += segment.Block.Length;
    //                ++k;
    //            }

    //            ++i;
    //        }

    //        Assert.Equal(expectedNumSegments, totalSegments);
    //        Assert.Equal(expectedNumSegments, totalFileSizeSegments);
    //    }

    //    private static async Task<IFolder> RecreateFolder(string rootFolder)
    //    {
    //        IFolder folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(rootFolder, CreationCollisionOption.ReplaceExisting);
    //        await folder.DeleteAsync();
    //        folder = await FileSystem.Current.LocalStorage.CreateFolderAsync(rootFolder, CreationCollisionOption.ReplaceExisting);
    //        return folder;
    //    }
    //}
}