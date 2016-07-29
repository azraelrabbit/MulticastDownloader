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
        private const int Ipv6Overhead = 40;
        private const int Ipv4Overhead = 20;
        private const int UdpOverhead = 8;

        [ProtoMember(20)]
        internal string MulticastAddress { get; set; }

        [ProtoMember(21)]
        internal int MulticastPort { get; set; }

        [ProtoMember(22)]
        internal bool Ipv6 { get; set; }

        [ProtoMember(23)]
        internal int Mtu { get; set; }

        [ProtoMember(24)]
        internal int MulticastBurstLength { get; set; }

        [ProtoMember(25)]
        internal FileHeader[] Files { get; set; }

        [ProtoMember(26)]
        internal long WaveNumber { get; set; }

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

        // The maximum size of the encoded data in individual blocks being transmitted.
        internal static int GetBlockSize(int mtu, bool ipv6)
        {
            return mtu - UdpOverhead - (ipv6 ? Ipv6Overhead : Ipv4Overhead);
        }
    }
}
