// <copyright file="Response.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.IO;
    using System.Security;
    using ProtoBuf;

    [ProtoContract]
    internal class Response
    {
        [ProtoMember(1)]
        internal ResponseId ResponseType { get; set; }

        [ProtoMember(2)]
        internal string Message { get; set; }

        internal void ThrowIfFailed()
        {
            switch (this.ResponseType)
            {
                case ResponseId.Ok:
                    // Do nothing
                    return;
                case ResponseId.PathNotFound:
                    throw new FileNotFoundException(this.Message);
                case ResponseId.AccessDenied:
                    throw new UnauthorizedAccessException(this.Message);
                case ResponseId.InvalidOperation:
                case ResponseId.Failed:
                    throw new InvalidOperationException(this.Message);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
