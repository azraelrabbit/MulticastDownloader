// <copyright file="ClientConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System.Threading.Tasks;
    using Common.Logging;
    using Sockets.Plugin;

    // Represent a wrapper around client connection state.
    internal class ClientConnection : SessionConnectionBase
    {
        private ILog log = LogManager.GetLogger<ClientConnection>();

        internal ClientConnection(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal async Task InitTcpClient()
        {
            this.log.DebugFormat("Connecting to {0}", this.UriParameters);
            this.SetTcpSession(new TcpSocketClient());
            await this.TcpSession.ConnectAsync(this.UriParameters.Hostname, this.UriParameters.Port, this.UriParameters.UseTls);
            this.log.DebugFormat("Connected to {0}", this.UriParameters);
        }

        internal async Task<ClientUdpReader> CreateReader(string multicastAddress, int port)
        {
            ClientUdpReader reader = new ClientUdpReader(this.UriParameters, this.Settings);
            await reader.InitMulticastClient(multicastAddress, port);
            return reader;
        }
    }
}
