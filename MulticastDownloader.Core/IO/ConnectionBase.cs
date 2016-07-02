// <copyright file="ConnectionBase.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using Properties;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class ConnectionBase : IDisposable
    {
        private static readonly int SessionTimeout = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
        private ILog log = LogManager.GetLogger<ConnectionBase>();
        private UdpSocketMulticastClient multicastClient = new UdpSocketMulticastClient();
        private bool tcpListen;
        private bool udpListen;
        private bool disposed;

        internal ConnectionBase(UriParameters parms, IEncoder encoder, int ttl)
        {
            Contract.Requires(parms != null);
            Contract.Requires(encoder != null);
            Contract.Requires(ttl > 0);
            this.UriParameters = parms;
            this.Encoder = encoder;
            this.Ttl = ttl;
        }

        internal UriParameters UriParameters
        {
            get;
            private set;
        }

        internal int Ttl
        {
            get;
            private set;
        }

        internal UdpSocketMulticastClient MulticastClient
        {
            get
            {
                return this.multicastClient;
            }
        }

        internal IEncoder Encoder
        {
            get;
            private set;
        }

        internal ITcpSocketClient TcpSession
        {
            get;
            private set;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        internal void SetTcpSession(ITcpSocketClient client)
        {
            Contract.Requires(client != null);
            this.tcpListen = true;
            this.TcpSession = client;
            this.TcpSession.ReadStream.ReadTimeout = SessionTimeout;
            this.TcpSession.WriteStream.WriteTimeout = SessionTimeout;
        }

        internal async Task InitMulticastClient(string localAddress, string multicastAddress, int port)
        {
            this.udpListen = true;
            this.multicastClient.TTL = this.Ttl;
            await this.multicastClient.JoinMulticastGroupAsync(multicastAddress, port);
        }

        internal virtual async Task Close()
        {
            this.log.Debug("Closing connection");
            if (this.tcpListen)
            {
                await this.TcpSession.DisconnectAsync();
            }

            if (this.udpListen)
            {
                await this.multicastClient.DisconnectAsync();
            }
        }

        internal async Task SendSession<T>(T data, CancellationToken token)
            where T : class
        {
            Contract.Requires(this.TcpSession != null);
            Contract.Requires(data != null);
            Task<bool> t0 = Task.Run(() =>
            {
                Serializer.SerializeWithLengthPrefix(this.TcpSession.WriteStream, data, PrefixStyle.Fixed32);
                return true;
            });

            await t0.WaitWithCancellation(token);
        }

        internal async Task<T> ReceiveSession<T>(CancellationToken token)
            where T : class
        {
            Contract.Requires(this.TcpSession != null);
            Task<T> t0 = Task.Run(() =>
            {
                return Serializer.DeserializeWithLengthPrefix<T>(this.TcpSession.ReadStream, PrefixStyle.Fixed32);
            });

            return await t0.WaitWithCancellation(token);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                this.disposed = true;
                if (this.TcpSession != null)
                {
                    this.TcpSession.Dispose();
                }

                if (this.multicastClient != null)
                {
                    this.multicastClient.Dispose();
                }
            }
        }
    }
}
