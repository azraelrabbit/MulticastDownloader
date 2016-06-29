// <copyright file="WaveCompleteResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class WaveCompleteResponse : Response
    {
        internal WaveCompleteResponse()
        {
            this.ResponseType = ResponseId.WaveComplete;
        }

        // The file range
        [ProtoMember(2)]
        internal int WaveId { get; set; }
    }
}
