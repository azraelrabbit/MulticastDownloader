// <copyright file="FileSystemTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using System;
    using System.Threading.Tasks;
    using Core.Session;
    using PCLStorage;
    using Xunit;
    using IO = System.IO;

    public class FileSystemTest
    {
        [Theory]
        [InlineData("test1.txt", "test2.txt", "12345")]
        [InlineData("test1.txt", "test2.txt", "45678")]
        public async Task IFileCanGetChecksum(string file1, string file2, string testData)
        {
            IFile f1 = await FileSystem.Current.LocalStorage.CreateFileAsync(file1, CreationCollisionOption.ReplaceExisting);
            await f1.WriteAllTextAsync(testData);
            IFile f2 = await FileSystem.Current.LocalStorage.CreateFileAsync(file2, CreationCollisionOption.ReplaceExisting);
            await f2.WriteAllTextAsync(testData);
            int check1 = await f1.GetChecksum();
            int check2 = await f2.GetChecksum();
            Assert.Equal(check1, check2);
            Assert.NotEqual(0, check1);
        }

        [Theory]
        [InlineData("test1.txt", new long[] { 1, 2, 10, 9, 7, 0 })]
        public async Task IFileCanResize(string file, long[] sizes)
        {
            foreach (long size in sizes)
            {
                IFile f = await FileSystem.Current.LocalStorage.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting);
                await f.Resize(size);
                using (IO.Stream s = await f.OpenAsync(FileAccess.Read))
                {
                    Assert.Equal(size, s.Length);
                    Assert.Equal(0, s.Position);
                }
            }
        }

        [Theory]
        [InlineData("foo.txt", true)]
        [InlineData("bar.txt", false)]
        public async Task IFolderCanCreateAndDeleteFile(string fileName, bool openRead)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            try
            {
                IFile file = await folder.Create(fileName, openRead);
                Assert.NotNull(file);
                Assert.False(openRead);
            }
            catch (Exception)
            {
                Assert.True(openRead);
            }

            if (!openRead)
            {
                bool deleted = await folder.Delete(fileName);
                Assert.True(deleted);
            }
        }
    }
}
