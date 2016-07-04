// <copyright file="Response.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Session
{
    using System;
    using System.IO;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(1, typeof(SessionJoinResponse))]
    [ProtoInclude(2, typeof(WaveCompleteResponse))]
    internal class Response
    {
        [ProtoMember(1)]
        internal ResponseId ResponseType { get; set; }

        [ProtoMember(2)]
        internal string Message { get; set; }

        internal static T CreateFailure<T>(ResponseId responseType, string message)
            where T : Response, new()
        {
            T ret = new T();
            ret.ResponseType = responseType;
            ret.Message = message;
            return ret;
        }

        internal void ThrowIfFailed()
        {
            switch (this.ResponseType)
            {
                case ResponseId.WaveComplete:
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
