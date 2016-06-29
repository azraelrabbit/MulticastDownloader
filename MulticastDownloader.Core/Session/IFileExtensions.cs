// <copyright file="IFileExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using PCLStorage;

    /// <summary>
    /// Extension methods for <see cref="IFile"/>
    /// </summary>
    public static class IFileExtensions
    {
        /// <summary>
        /// Gets the checksum for the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>A checksum value.</returns>
        public static async Task<int> GetChecksum(this IFile file)
        {
            byte[] buffer = new byte[4];
            int ret = 0;
            using (Stream s = await file.OpenAsync(FileAccess.Read))
            {
                for (int i = 0; i < buffer.Length; ++i)
                {
                    buffer[i] = 0;
                }

                await s.ReadAsync(buffer, 0, buffer.Length);
                int v = BitConverter.ToInt32(buffer, 0);
                ret += v;
            }

            return ~ret;
        }

        /// <summary>
        /// Resizes the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="size">The size.</param>
        /// <returns>A <see cref="Task"/> object.</returns>
        public static async Task Resize(this IFile file, long size)
        {
            using (Stream s = await file.OpenAsync(FileAccess.ReadAndWrite))
            {
                s.SetLength(0);
                s.SetLength(size);
                s.Position = 0;
                s.Flush();
            }
        }
    }
}
