// <copyright file="SessionConnectionBase.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using Org.BouncyCastle.Crypto.Tls;
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class SessionConnectionBase : ConnectionBase
    {
#pragma warning disable CA2213
        private Stream tlsStream;
        private Stream outStream;
        private Stream inStream;
#pragma warning restore CA2213
        private ILog log = LogManager.GetLogger<ClientConnection>();
        private bool tcpListen;
        private bool disposed;

        internal SessionConnectionBase(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
        }

        internal Stream TlsStream
        {
            get
            {
                return this.tlsStream;
            }

            set
            {
                this.tlsStream = value;
                if (this.tlsStream == null)
                {
                    this.outStream = this.TcpSession.WriteStream;
                    this.inStream = this.TcpSession.ReadStream;
                }
                else
                {
                    this.outStream = this.inStream = this.tlsStream;
                }
            }
        }

        internal ITcpSocketClient TcpSession
        {
            get;
            private set;
        }

        internal override async Task Close()
        {
            await base.Close();
            if (this.tcpListen)
            {
                await this.TcpSession.DisconnectAsync();
            }
        }

        internal void Send<T>(T data, CancellationToken token)
            where T : class
        {
            Contract.Requires(this.TcpSession != null);
            Contract.Requires(data != null);
            using (CancellationTokenRegistration ctr = token.Register(() => this.outStream.Dispose()))
            {
                Serializer.SerializeWithLengthPrefix(this.outStream, data, PrefixStyle.Base128);
            }
        }

        internal T Receive<T>(CancellationToken token)
            where T : class
        {
            Contract.Requires(this.TcpSession != null);
            using (CancellationTokenRegistration ctr = token.Register(() => this.inStream.Dispose()))
            {
                return Serializer.DeserializeWithLengthPrefix<T>(this.inStream, PrefixStyle.Base128);
            }
        }

        internal void SetTcpSession(ITcpSocketClient client)
        {
            Contract.Requires(client != null);
            this.tcpListen = true;
            this.TcpSession = client;
            this.TcpSession.ReadStream.ReadTimeout = (int)this.Settings.ReadTimeout.TotalMilliseconds;
            this.outStream = this.TcpSession.ReadStream;
            this.inStream = this.TcpSession.WriteStream;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!this.disposed)
            {
                this.disposed = true;
                if (this.TcpSession != null)
                {
                    this.TcpSession.Dispose();
                }

                if (this.TlsStream != null)
                {
                    try
                    {
                        this.TlsStream.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }
    }
}
