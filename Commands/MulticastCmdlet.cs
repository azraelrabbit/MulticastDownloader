// <copyright file="MulticastCmdlet.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Threading;
    using System.Threading.Tasks;
    using Core;
    using Core.Cryptography;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent a base class for Multicast commandlets.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Commands.AsyncCmdlet" />
    public abstract class MulticastCmdlet : AsyncCmdlet
    {
        private CancellationTokenSource ctrlCCanceler = new CancellationTokenSource();
        private bool canceled = false;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastCmdlet"/> class.
        /// </summary>
        public MulticastCmdlet()
        {
            this.MulticastBufferSize = 1 << 20;
            this.ReadTimeout = TimeSpan.FromMinutes(10);
            this.Ttl = 1;
            this.UpdateInterval = TimeSpan.FromMilliseconds(1000);
            bool canceled = false;

            Console.CancelKeyPress += (o, e) =>
            {
                if (!canceled)
                {
                    Console.Out.WriteLine(Resources.CtrlCPressed);
                    canceled = true;
                    e.Cancel = true;
                    this.ctrlCCanceler.Cancel();
                }
            };
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public CancellationToken Token
        {
            get
            {
                return this.ctrlCCanceler.Token;
            }
        }

        /// <summary>
        /// Gets or sets the size of the multicast buffer, in bytes, allocated to temporarily storing outbound or inbound multicast data.
        /// <para type="description">The size of the multicast buffer, in bytes, allocated to temporarily storing outbound or inbound multicast data.</para>
        /// </summary>
        /// <value>
        /// The size of the multicast buffer.
        /// </value>
        [Parameter]
        public int MulticastBufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TCP read timeout used for the session.
        /// <para type="description">The TCP read timeout used for the session.</para>
        /// </summary>
        /// <value>
        /// The read timeout.
        /// </value>
        [Parameter]
        public TimeSpan ReadTimeout
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the multicast URI.
        /// <para type="description">The multicast URI.</para>
        /// </summary>
        /// <value>
        /// The multicast URI.
        /// </value>
        [Parameter(HelpMessage = "The multicast URI", Mandatory = true)]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the progress update interval.
        /// <para type="description">The progress update interval.</para>
        /// </summary>
        /// <value>
        /// The update interval.
        /// </value>
        [Parameter]
        public TimeSpan UpdateInterval
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the TTL.
        /// <para type="description">The multicast TTL.</para>
        /// </summary>
        /// <value>
        /// The TTL.
        /// </value>
        /// <remarks>
        /// A value of 1 should be used if you only want to multicast to clients on your router.
        /// </remarks>
        [Parameter]
        public int Ttl
        {
            get;
            set;
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
                if (disposing)
                {
                    if (this.ctrlCCanceler != null)
                    {
                        this.ctrlCCanceler.Dispose();
                    }
                }
            }
        }
    }
}
