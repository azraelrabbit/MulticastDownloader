// <copyright file="IMulticastServerSettingsExtensionMethods.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    using System;
    using Properties;

    /// <summary>
    /// Extension methods for <see cref="IMulticastServerSettings"/>
    /// </summary>
    public static class IMulticastServerSettingsExtensionMethods
    {
        /// <summary>
        /// Validates the specified server settings.
        /// </summary>
        /// <param name="serverSettings">The server settings.</param>
        public static void Validate(this IMulticastServerSettings serverSettings)
        {
            if (serverSettings.MaxBytesPerSecond < (10 << 20))
            {
                throw new InvalidOperationException(Resources.MustSpecifyAtLeast10MbitASecond);
            }

            if (serverSettings.MaxConnections < 1)
            {
                throw new InvalidOperationException(Resources.MaxConnectionsTooLow);
            }

            if (serverSettings.MaxSessions < 1)
            {
                throw new InvalidOperationException(Resources.MaxSessionsTooLow);
            }

            if (serverSettings.Mtu < 576)
            {
                throw new InvalidOperationException(Resources.MtuTooLow);
            }

            if (string.IsNullOrEmpty(serverSettings.MulticastAddress) || serverSettings.MulticastStartPort < 1)
            {
                throw new InvalidOperationException(Resources.MustSpecifyMulticastAddressAndStartPort);
            }

            if (serverSettings.MulticastBurstLength < 1)
            {
                throw new InvalidOperationException(Resources.InvalidMulticastBurstLength);
            }
        }
    }
}
