// <copyright file="FileEnumerator.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using PCLStorage;

    /// <summary>
    /// Represent a recursive file enumerator.
    /// </summary>
    public static class FileEnumerator
    {
        /// <summary>
        /// An inclusion spec that includes any file.
        /// </summary>
        public static readonly Func<IFile, bool> IncludeAny = (f) => true;

        /// <summary>
        /// Gets the files from  under the specified root folder.
        /// </summary>
        /// <param name="rootFolder">The root folder.</param>
        /// <param name="recurse">Whether or not to recursively evaluate folders.</param>
        /// <param name="inclusionSpec">The inclusion spec.</param>
        /// <returns>A collection of files found under the given path matching the recurse option and inclusion spec.</returns>
        public static async Task<ICollection<string>> GetFilesFromPath(IFolder rootFolder, bool recurse, Func<IFile, bool> inclusionSpec)
        {
            List<string> ret = new List<string>();
            Queue<string> folderQueue = new Queue<string>();
            folderQueue.Enqueue(string.Empty);
            while (folderQueue.Count > 0)
            {
                string curFolder = folderQueue.Dequeue();
                IFolder folder = await rootFolder.GetFolderAsync(curFolder);
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
                    string fileName = PathAppend(curFolder, file.Name);
                    ret.Add(fileName);
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
