// <copyright file="SessionJoinResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    // Represent a session join response.
    [ProtoContract]
    internal class SessionJoinResponse : Response
    {
        [ProtoMember(1)]
        internal string MulticastAddress { get; set; }

        [ProtoMember(2)]
        internal int MulticastPort { get; set; }

        [ProtoMember(3)]
        internal FileHeader[] Files { get; set; }

        internal long CountSegments
        {
            get
            {
                if (this.Files != null)
                {
                    long ret = 0;
                    foreach (FileHeader header in this.Files)
                    {
                        if (header.Blocks != null)
                        {
                            ret += header.Blocks.Length;
                        }
                    }

                    return ret;
                }

                return 0;
            }
        }
    }
}
