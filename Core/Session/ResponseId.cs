// <copyright file="ResponseId.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    // Represent a session response
    internal enum ResponseId
    {
        Ok,
        Failed,
        AccessDenied,
        InvalidOperation,
        PathNotFound,
        WaveComplete
    }
}
