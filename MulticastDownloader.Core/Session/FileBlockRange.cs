// <copyright file="FileBlockRange.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class FileBlockRange
    {
        [ProtoMember(1)]
        internal long Offset { get; set; }

        [ProtoMember(2)]
        internal long Length { get; set; }

        [ProtoMember(3)]
        internal long SegmentId { get; set; }
    }
}
