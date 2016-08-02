// <copyright file="Constants.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using System.Text;

    internal class Constants
    {
        internal const int MaxIntervals = 10;
        internal const int PacketUpdateInterval = 1000;
        internal const int EncoderSize = 256;
        internal static readonly TimeSpan ReadDelay = TimeSpan.FromMilliseconds(200);
        internal static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(30);
        internal static readonly byte[] ResponseId = Encoding.UTF8.GetBytes("client");
    }
}
