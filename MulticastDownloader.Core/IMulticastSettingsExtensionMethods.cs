// <copyright file="IMulticastSettingsExtensionMethods.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using Properties;

    /// <summary>
    /// Extension methods for <see cref="IMulticastSettings"/>
    /// </summary>
    public static class IMulticastSettingsExtensionMethods
    {
        /// <summary>
        /// Validates the specified settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public static void Validate(this IMulticastSettings settings)
        {
            if (settings.MulticastBufferSize < 576)
            {
                throw new InvalidOperationException(Resources.InvalidMulticastBufferSize);
            }

            if (settings.RootFolder == null)
            {
                throw new InvalidOperationException(Resources.InvalidRootFolder);
            }

            if (settings.Ttl < 1)
            {
                throw new InvalidOperationException(Resources.InvalidTtl);
            }
        }
    }
}
