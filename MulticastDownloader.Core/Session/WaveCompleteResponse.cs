// <copyright file="WaveCompleteResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class WaveCompleteResponse : Response
    {
        // The next wave number.
        [ProtoMember(1)]
        internal long WaveNumber { get; set; }

        [ProtoMember(2)]
        internal double ReceptionRate { get; set; }
    }
}
