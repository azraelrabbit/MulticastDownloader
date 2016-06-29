// <copyright file="FileSegment.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class FileSegment
    {
        // Last segment in the packet?
        [ProtoMember(1)]
        internal bool LastSegment { get; set; }

        [ProtoMember(2)]
        internal long SegmentId { get; set; }
    }
}
