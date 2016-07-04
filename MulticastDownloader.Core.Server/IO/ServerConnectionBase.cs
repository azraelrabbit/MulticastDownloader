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
    using Core.IO;
    using Cryptography;
    using PCLStorage;
    using Properties;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ServerConnectionBase : ConnectionBase
    {
        private const int Ipv6Overhead = 40;
        private const int Ipv4Overhead = 20;
        private const int UdpOverhead = 8;
        private int blockSize;

        internal ServerConnectionBase(UriParameters parms, IMulticastSettings settings, IMulticastServerSettings serverSettings)
            : base(parms, settings)
        {
            Contract.Requires(serverSettings != null);
            this.ServerSettings = serverSettings;
            this.blockSize = this.ServerSettings.Mtu - UdpOverhead - (this.ServerSettings.Ipv6 ? Ipv6Overhead : Ipv4Overhead);
            if (this.Settings.Encoder != null)
            {
                int unencodedSize = this.blockSize;
                while (this.Settings.Encoder.GetEncodedOutputLength(unencodedSize) > this.blockSize)
                {
                    --unencodedSize;
                }

                Contract.Assert(unencodedSize > 0);
                this.BlockSize = unencodedSize;
            }
        }

        internal IMulticastServerSettings ServerSettings
        {
            get;
            private set;
        }

        internal int BlockSize
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
