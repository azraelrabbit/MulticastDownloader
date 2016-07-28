// <copyright file="BoxedLong.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    internal class BoxedLong
    {
        internal BoxedLong(long seq)
        {
            this.Value = seq;
        }

        internal long Value
        {
            get;
            private set;
        }
    }
}
