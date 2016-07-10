// <copyright file="ServerConnectionBase.cs" company="MS">
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
    using Core.Cryptography;
    using Core.IO;
    using Cryptography;
    using PCLStorage;
    using Properties;
    using ProtoBuf;
    using Session;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerConnectionBase : ConnectionBase
    {
        internal ServerConnectionBase(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : base(parms, settings)
        {
            Contract.Requires(serverSettings != null);
            if (serverSettings.Ipv6)
            {
                throw new ArgumentException(Resources.Ipv6MulticastNotSupported, "serverSettings");
            }

            this.ServerSettings = serverSettings;
        }

        internal IMulticastServerSettings ServerSettings
        {
            get;
            private set;
        }

        internal async Task<ICommsInterface> GetCommsInterface()
        {
            if (this.ServerSettings.InterfaceName != null)
            {
                ICommsInterface multicastOn = null;
                foreach (ICommsInterface commsInterface in await CommsInterface.GetAllInterfacesAsync())
                {
                    if (commsInterface.Name == this.ServerSettings.InterfaceName)
                    {
                        return multicastOn;
                    }
                }

                throw new InvalidOperationException(Resources.CouldNotFindInterface);
            }

            return null;
        }
    }
}
