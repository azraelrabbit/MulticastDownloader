// <copyright file="Challenge.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class Challenge
    {
        [ProtoMember(1)]
        internal byte[] ChallengKey { get; set; }
    }
}
