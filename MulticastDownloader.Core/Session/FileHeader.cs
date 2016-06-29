// <copyright file="FileHeader.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class FileHeader
    {
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
    }
}
