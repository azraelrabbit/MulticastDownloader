// <copyright file="SegmentHeader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    // Represent a segment of file data.
    [ProtoContract]
    internal class SegmentHeader
    {
        // The file session id
        [ProtoMember(1)]
        internal int SessionId { get; set; }

        // The file range
        [ProtoMember(2)]
        internal int WaveId { get; set; }

        // The file block range
        [ProtoMember(3)]
        internal long BlockId { get; set; }
    }
}
