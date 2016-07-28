// <copyright file="ServerConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Server.IO
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.IO;
    using Cryptography;
    using Org.BouncyCastle.Crypto.Tls;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerConnection : SessionConnectionBase
    {
        internal ServerConnection(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal void AcceptTls(byte[] psk)
        {
            Contract.Requires(this.UriParameters.UseTls);
            TlsServerProtocol serverProtocol = this.TcpSession.AcceptTls(psk);
            this.TlsStream = serverProtocol.Stream;
        }
    }
}
