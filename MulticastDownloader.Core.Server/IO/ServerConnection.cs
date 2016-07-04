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
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerConnection : SessionConnectionBase
    {
        private ILog log = LogManager.GetLogger<ServerConnection>();

        internal ServerConnection(UriParameters parms, IMulticastSettings settings, ITcpSocketClient client)
            : base(parms, settings)
        {
            Contract.Requires(client != null);
            this.SetTcpSession(client);
        }
    }
}
