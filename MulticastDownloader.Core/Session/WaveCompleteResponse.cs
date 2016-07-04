// <copyright file="WaveCompleteResponse.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    [ProtoContract]
    internal class WaveCompleteResponse : Response
    {
        // True if the server is transmitting the rest of the session blocks to the client
        [ProtoMember(1)]
        internal bool DirectDownload { get; set; }
    }
}
