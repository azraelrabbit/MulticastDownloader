// <copyright file="StartMulticastDownloadCommand.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core;
    using Core.Cryptography;
    using Core.IO;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// <para type="synopsis">Starts a multicast download.</para>
    /// <para type="description">This starts a multicast download using the specified parameters. This call will block until the download is complete.</para>
    /// </summary>
    /// <seealso cref="Cmdlet" />
    /// <seealso cref="MulticastClient"/>
    [Cmdlet(VerbsLifecycle.Start, "MulticastDownload")]
    public class StartMulticastDownloadCommand : Cmdlet, IMulticastSettings
    {
        private IFolder rootFolder = FileSystem.Current.LocalStorage;
        private string destinationPath;
        private IEncoderFactory encoderFactory;
        private string passPhrase;
        private string publicKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartMulticastDownloadCommand"/> class.
        /// </summary>
        public StartMulticastDownloadCommand()
        {
            this.MulticastBufferSize = 1 << 20;
            this.ReadTimeout = TimeSpan.FromMinutes(10);
            this.Ttl = 1;
            this.UpdateInterval = TimeSpan.FromMilliseconds(1000);
        }

        /// <summary>
        /// Gets the encoder used for encoding data and authorizing clients.
        /// </summary>
        /// <value>
        /// The encoder.
        /// </value>
        /// <remarks>
        /// Encoding data can be used to authorize clients to receive data as well as to guarantee data can't be viewed by other users on your network, however
        /// encoding data can decrease transfer rates. This value can be null if you don't want to use encoded data. See the <see cref="N:MS.MulticastDownloader.Core.Cryptography" /> namespace
        /// for built-in encoders.
        /// </remarks>
        public IEncoderFactory Encoder
        {
            get
            {
                return this.encoderFactory;
            }
        }

        /// <summary>
        /// Gets or sets the pass phrase used for encoding.
        /// <para type="description">The pass phrase used for encoding.</para>
        /// </summary>
        /// <value>
        /// The pass phrase.
        /// </value>
        [Parameter(ParameterSetName = "PassPhrase")]
        public string PassPhrase
        {
            get
            {
                return this.passPhrase;
            }

            set
            {
                this.passPhrase = value;
                this.publicKey = null;
            }
        }

        /// <summary>
        /// Gets or sets the public key used for encoding.
        /// <para type="description">The public key used for encoding.</para>
        /// </summary>
        /// <value>
        /// The public key.
        /// </value>
        [Parameter(ParameterSetName = "PublicKey")]
        public string PublicKey
        {
            get
            {
                return this.publicKey;
            }

            set
            {
                this.publicKey = value;
                this.passPhrase = null;
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
        /// Gets the multicast root folder.
        /// <para type="description">The multicast root folder.</para>
        /// </summary>
        /// <value>
        /// The root folder.
        /// </value>
        /// <remarks>
        /// The root folder is the path under which the <see cref="P:MS.MulticastDownloader.Core.UriParameters.Path" /> member refers for an individual session join request.
        /// </remarks>
        public IFolder RootFolder
        {
            get
            {
                return this.rootFolder;
            }
        }

        /// <summary>
        /// Gets or sets the root folder into which the multicast URI will be copied.
        /// <para type="description">The root folder into which the multicast URI will be copied.</para>
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        [Parameter(Mandatory = true, HelpMessage = "The destination path")]
        public string DestinationPath
        {
            get
            {
                return this.destinationPath;
            }

            set
            {
                this.destinationPath = value;
            }
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
        /// Gets or sets the log level.
        /// <para type="description">The log level.</para>
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        [Parameter]
        public LogLevel LogLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the log file.
        /// <para type="description">The log file.</para>
        /// </summary>
        /// <value>
        /// The log file.
        /// </value>
        [Parameter]
        public string LogFile
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
        /// Ends the processing.
        /// </summary>
        protected override async void EndProcessing()
        {
            base.EndProcessing();
            LogManager.Adapter = new ConsoleLoggerFactoryAdapter(this.LogLevel, this.LogFile);
            this.rootFolder = await FileSystem.Current.LocalStorage.GetFolderAsync(this.destinationPath);
            if (this.rootFolder == null)
            {
                this.rootFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync(this.destinationPath, CreationCollisionOption.ReplaceExisting);
            }

            if (!string.IsNullOrEmpty(this.passPhrase))
            {
                this.encoderFactory = new PassphraseEncoderFactory(this.passPhrase, Encoding.Unicode);
            }

            if (!string.IsNullOrEmpty(this.publicKey))
            {
                this.encoderFactory = await AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, this.publicKey, AsymmetricSecretFlags.None);
            }

            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                bool canceled = false;
                Console.CancelKeyPress += (o, e) =>
                {
                    if (!canceled)
                    {
                        Console.Out.WriteLine(Resources.CtrlCPressed);
                        canceled = true;
                        e.Cancel = true;
                        cts.Cancel();
                    }
                };
                using (MulticastClient client = new MulticastClient(this.Uri, this))
                {
                    Task transferTask = client.StartTransfer(cts.Token);
                    while (!transferTask.IsCompleted && !transferTask.IsCanceled && !transferTask.IsFaulted)
                    {
                        Thread.Sleep(this.UpdateInterval);
                        this.WriteTransferProgress(0, client);
                        this.WriteTransferReception(0, client);
                    }

                    this.WriteTransferProgressComplete(0, client);
                    this.WriteTransferReceptionComplete(0, client);
                    await transferTask;
                }
            }
        }
    }
}
