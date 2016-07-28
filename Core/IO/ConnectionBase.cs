// <copyright file="ConnectionBase.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.IO
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
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
        private ILog log = LogManager.GetLogger<ConnectionBase>();

        internal ConnectionBase(UriParameters parms, IMulticastSettings settings)
        {
            Contract.Requires(parms != null && settings != null);
            this.UriParameters = parms;
            this.Settings = settings;
        }

        internal UriParameters UriParameters
        {
            get;
            private set;
        }

        internal IMulticastSettings Settings
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

        internal virtual Task Close()
        {
            return Task.Run(() => this.log.DebugFormat(CultureInfo.InvariantCulture, "{0}: Closing connection...", this.GetType()));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
