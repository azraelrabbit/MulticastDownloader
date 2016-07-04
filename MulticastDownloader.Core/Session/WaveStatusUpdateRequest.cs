// <copyright file="WaveStatusUpdateRequest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    // Sent by the client as soon as it joins a session, and any time afterwards it receives a new wave notification
    [ProtoContract]
    internal class WaveStatusUpdateRequest
    {
        [ProtoMember(1)]
        internal int WaveNumber { get; set; }
    }
}
