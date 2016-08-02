// <copyright file="FileSegment.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Linq;
    using ProtoBuf;

    [ProtoContract]
    internal class FileSegment : IEquatable<FileSegment>
    {
        [ProtoMember(1)]
        internal long SegmentId { get; set; }

        [ProtoMember(2)]
        internal byte[] Data { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is FileSegment)
            {
                return this.Equals(obj as FileSegment);
            }

            return base.Equals(obj);
        }

        public bool Equals(FileSegment other)
        {
            return this.SegmentId == other.SegmentId && (this.Data == other.Data || this.Data != null ? this.Data.SequenceEqual(other.Data) : false);
        }

        public override int GetHashCode()
        {
            return this.SegmentId.GetHashCode() + (this.Data != null ? this.Data.Sum((b) => b) : 0);
        }

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
