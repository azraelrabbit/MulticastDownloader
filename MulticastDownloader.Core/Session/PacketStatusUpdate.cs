// <copyright file="PacketStatusUpdate.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(1, typeof(WaveStatusUpdate))]
    internal class PacketStatusUpdate
    {
        [ProtoMember(1)]
        internal bool LeavingSession { get; set; }

        [ProtoMember(2)]
        internal long SegmentsLeft { get; set; }
    }
}
