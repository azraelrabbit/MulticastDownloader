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
    using ProtoBuf;
    using Sockets.Plugin;
    using Sockets.Plugin.Abstractions;

    internal class SessionConnectionBase : ConnectionBase
    {
        private ILog log = LogManager.GetLogger<ClientConnection>();
        private bool tcpListen;
        private bool disposed;

        internal SessionConnectionBase(UriParameters parms, IMulticastSettings settings)
            : base(parms, settings)
        {
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

        internal void SetTcpSession(ITcpSocketClient client)
        {
            Contract.Requires(client != null);
            this.tcpListen = true;
            this.TcpSession = client;
            this.TcpSession.ReadStream.ReadTimeout = (int)this.Settings.ReadTimeout.TotalMilliseconds;
            this.TcpSession.WriteStream.WriteTimeout = (int)this.Settings.ReadTimeout.TotalMilliseconds;
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
            }
        }
    }
}
