// <copyright file="ClientConnection.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using Org.BouncyCastle.Crypto.Tls;
    using Session;
    using Sockets.Plugin;

    // Represent a wrapper around client connection state.
    internal class ClientConnection : SessionConnectionBase
    {
        private ILog log = LogManager.GetLogger<ClientConnection>();
        private ITcpSocketClientExtensions.TcpPskTlsClient tlsClient;

        internal ClientConnection(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal async Task Connect()
        {
            this.log.DebugFormat("Connecting to {0}", this.UriParameters);
            TcpSocketClient client = new TcpSocketClient();
            await client.ConnectAsync(this.UriParameters.Hostname, this.UriParameters.Port);
            this.log.DebugFormat("Connected to {0}", this.UriParameters);
            this.SetTcpSession(client);
        }

        internal void ConnectTls(byte[] psk)
        {
            Contract.Requires(this.UriParameters.UseTls);
            TlsSession session = null;
            if (this.tlsClient != null)
            {
                session = this.tlsClient.GetSessionToResume();
            }

            this.tlsClient = new ITcpSocketClientExtensions.TcpPskTlsClient(session, psk);
            TlsClientProtocol tlsClient = this.TcpSession.ConnectTls(this.tlsClient);
            this.TlsStream = tlsClient.Stream;
        }

        internal async Task<UdpReader> JoinMulticastServer(IUdpMulticast udpMulticast, SessionJoinResponse response, IEncoderFactory encoder)
        {
            UdpReader reader = new UdpReader(this.UriParameters, this.Settings, udpMulticast);
            await reader.JoinMulticastServer(response, encoder);
            return reader;
        }
    }
}
