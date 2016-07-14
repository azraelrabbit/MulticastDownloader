// <copyright file="SeqNum.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    internal class SeqNum
    {
        internal SeqNum(long seq)
        {
            this.Seq = seq;
        }

        internal long Seq
        {
            get;
            private set;
        }
    }
}
