// <copyright file="PacketStatusUpdateResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class PacketStatusUpdateResponse : Response
    {
        [ProtoMember(10)]
        internal double ReceptionRate { get; set; }
    }
}
