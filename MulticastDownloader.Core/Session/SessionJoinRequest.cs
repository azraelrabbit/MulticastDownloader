// <copyright file="SessionJoinRequest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class SessionJoinRequest
    {
        [ProtoMember(1)]
        internal string Path { get; set; }
    }
}
