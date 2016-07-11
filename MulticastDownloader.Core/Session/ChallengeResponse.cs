// <copyright file="ChallengeResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class ChallengeResponse
    {
        [ProtoMember(1)]
        internal byte[] ChallengeKey { get; set; }
    }
}
