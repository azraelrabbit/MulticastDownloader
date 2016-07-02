// <copyright file="IFolderExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using PCLStorage;

    /// <summary>
    /// Extension methods for <see cref="IFolder"/>
    /// </summary>
    public static class IFolderExtensions
    {
        /// <summary>
        /// An inclusion spec that includes any file.
        /// </summary>
        public static readonly Func<IFile, bool> IncludeAny = (f) => true;

        /// <summary>
        /// Creates the specified file under the given folder
        /// </summary>
        /// <param name="folder">The file.</param>
        /// <param name="fileName">The folder.</param>
        /// <param name="openRead">True if the file will be opened for read access.</param>
        /// <returns>An <see cref="IFile"/> representing the given file.</returns>
        public static async Task<IFile> Create(this IFolder folder, string fileName, bool openRead)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            if (openRead)
            {
                return await folder.GetFileAsync(fileName);
            }
            else
            {
                string dirName = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(dirName))
                {
                    await folder.CreateFolderAsync(dirName, CreationCollisionOption.OpenIfExists);
                }

                return await folder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            }
        }

        /// <summary>
        /// Deletes the specified file under the given folder.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>A task object.</returns>
        public static async Task<bool> Delete(this IFolder folder, string fileName)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            try
            {
                IFile file = await folder.GetFileAsync(fileName);
                await file.DeleteAsync();
            }
            catch (FileNotFoundException)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the files from  under the specified root folder.
        /// </summary>
        /// <param name="rootFolder">The root folder.</param>
        /// <param name="recurse">Whether or not to recursively evaluate folders.</param>
        /// <param name="inclusionSpec">The inclusion spec.</param>
        /// <returns>A collection of files found under the given path matching the recurse option and inclusion spec.</returns>
        public static async Task<ICollection<string>> GetFilesFromPath(this IFolder rootFolder, bool recurse, Func<IFile, bool> inclusionSpec)
        {
            if (inclusionSpec == null)
            {
                throw new ArgumentNullException("inclusionSpec");
            }

            List<string> ret = new List<string>();
            Queue<string> folderQueue = new Queue<string>();
            folderQueue.Enqueue(string.Empty);
            while (folderQueue.Count > 0)
            {
                string curFolder = folderQueue.Dequeue();
                IFolder folder = string.IsNullOrEmpty(curFolder) ? rootFolder : await rootFolder.GetFolderAsync(curFolder);
                if (recurse)
                {
                    foreach (IFolder childFolder in await folder.GetFoldersAsync())
                    {
                        string folderName = PathAppend(curFolder, childFolder.Name);
                        folderQueue.Enqueue(folderName);
                    }
                }

                foreach (IFile file in await folder.GetFilesAsync())
                {
                    if (inclusionSpec(file))
                    {
                        string fileName = PathAppend(curFolder, file.Name);
                        ret.Add(fileName);
                    }
                }
            }

            return ret;
        }

        private static string PathAppend(string path, string extraPath)
        {
            if (string.IsNullOrEmpty(path))
            {
                return extraPath;
            }

            return path + PortablePath.DirectorySeparatorChar + extraPath;
        }
    }
}
