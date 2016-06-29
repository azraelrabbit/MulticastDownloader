// <copyright file="WaveStatusUpdate.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using ProtoBuf;

    // Sent by the client as soon as it joins a session, and any time afterwards it receives a new wave notification
    [ProtoContract]
    internal class WaveStatusUpdate : PacketStatusUpdate
    {
        internal WaveStatusUpdate()
        {
            this.WaveUpdate = true;
        }

        // The bit-vector mask of file sequences id's that the client has.
        [ProtoMember(1)]
        internal byte[] FileMask { get; set; }
    }
}
