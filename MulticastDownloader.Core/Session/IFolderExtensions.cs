// <copyright file="IFolderExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using PCLStorage;

    /// <summary>
    /// Extension methods for <see cref="IFolder"/>
    /// </summary>
    public static class IFolderExtensions
    {
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
    }
}
