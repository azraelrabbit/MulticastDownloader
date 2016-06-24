// <copyright file="Runtime.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Util
{
    using System;
    using PCLStorage;
    using Xunit;

    /// <summary>
    /// Utility functions
    /// </summary>
    public static class Runtime
    {
        public static bool HasFileSystem()
        {
            try
            {
                FileSystem.Current.LocalStorage.GetFilesAsync().Wait();
                return true;
            }
            catch (NotImplementedException)
            {
                return false;
            }
            catch (AggregateException ae)
            {
                if (ae.InnerException is NotImplementedException)
                {
                    return false;
                }

                throw;
            }
        }
    }
}
