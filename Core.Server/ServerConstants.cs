// <copyright file="ServerConstants.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server
{
    internal class ServerConstants
    {
        // Clients much respond to any request with the given interval
        internal const int MinBurstDelay = 1;
        internal const int StartBurstDelay = 10;
        internal const int MaxBurstDelay = 50;
        internal const double DecreaseThreshold = 0.98;
        internal const double IncreaseThreshold = 0.90;
    }
}
