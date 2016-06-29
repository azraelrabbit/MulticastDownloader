// <copyright file="FileSegment.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class FileSegment
    {
        [ProtoMember(1)]
        internal long SegmentId { get; set; }

        [ProtoMember(2)]
        internal byte[] Data { get; set; }

        // See https://developers.google.com/protocol-buffers/docs/encoding#non-varint-numbers

        // Determine the overhead for transmitting a segment of the given length.
        internal static long GetSegmentOverhead(long segmentId, long dataLength)
        {
            // 2 fields
            return 2 + SizeOfVarint(segmentId) + SizeOfVarint(dataLength);
        }

        // Determine the wire size of a varint.
        private static long SizeOfVarint(long value)
        {
            long ret = 0;
            while (value > 0)
            {
                value >>= 7;
                ++ret;
            }

            return ret;
        }
    }
}
