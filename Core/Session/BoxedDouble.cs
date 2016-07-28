// <copyright file="BoxedDouble.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    internal class BoxedDouble
    {
        internal BoxedDouble(double value)
        {
            this.Value = value;
        }

        internal double Value
        {
            get;
            private set;
        }
    }
}
