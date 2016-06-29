// <copyright file="FileHeader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Logging;
    using PCLStorage;
    using ProtoBuf;

    [ProtoContract]
    internal class FileHeader : IEquatable<FileHeader>
    {
        private static ILog log = LogManager.GetLogger<FileHeader>();

        [ProtoMember(1)]
        internal string Name { get; set; }

        internal long Length
        {
            get
            {
                if (this.Blocks != null)
                {
                    long ret = 0;
                    foreach (FileBlockRange fbr in this.Blocks)
                    {
                        ret += fbr.Length;
                    }

                    return ret;
                }

                return 0;
            }
        }

        [ProtoMember(2)]
        internal FileBlockRange[] Blocks { get; set; }

        [ProtoMember(3, DataFormat = DataFormat.FixedSize)]
        internal int Checksum { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is FileHeader)
            {
                return this.Equals(obj as FileHeader);
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
        public bool Equals(FileHeader other)
        {
            if (other != null && other != this)
            {
                return string.Compare(this.Name, other.Name, StringComparison.Ordinal) != 0
                    && this.Checksum == other.Checksum
                    && (this.Blocks == other.Blocks || this.Blocks.SequenceEqual(other.Blocks));
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
            int ret = (this.Name ?? string.Empty).GetHashCode() + this.Checksum;
            if (this.Blocks != null)
            {
                foreach (FileBlockRange fbr in this.Blocks)
                {
                    ret += fbr.GetHashCode();
                }
            }

            return ret;
        }
    }
}
