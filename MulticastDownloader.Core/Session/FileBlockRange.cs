// <copyright file="FileBlockRange.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using ProtoBuf;

    [ProtoContract]
    internal class FileBlockRange : IEquatable<FileBlockRange>
    {
        [ProtoMember(1)]
        internal long Offset { get; set; }

        [ProtoMember(2)]
        internal long Length { get; set; }

        [ProtoMember(3)]
        internal long SegmentId { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is FileBlockRange)
            {
                return this.Equals(obj as FileBlockRange);
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(FileBlockRange other)
        {
            if (other != null && other != this)
            {
                return this.Length == other.Length
                    && this.Offset == other.Offset
                    && this.SegmentId == other.SegmentId;
            }

            return other == this;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return this.Length.GetHashCode() + this.Offset.GetHashCode() + this.SegmentId.GetHashCode();
        }
    }
}
