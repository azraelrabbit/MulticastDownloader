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

        [ProtoMember(2)]
        internal byte[] ChallengeResponse { get; set; }

        // Application-defined state
        [ProtoMember(3)]
        internal int State { get; set; }
    }
}
