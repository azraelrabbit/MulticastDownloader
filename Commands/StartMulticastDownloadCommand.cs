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
    using System.Windows.Forms;
    using Common.Logging;
    using Core;
    using Core.Cryptography;
    using Core.IO;
    using PCLStorage;
    using Properties;
    using Status;

    /// <summary>
    /// <para type="synopsis">Starts a multicast download.</para>
    /// <para type="description">This starts a multicast download using the specified parameters. This call will block until the download is complete.</para>
    /// </summary>
    /// <seealso cref="Cmdlet" />
    /// <seealso cref="MulticastClient{TReader}"/>
    [Cmdlet(VerbsLifecycle.Start, "MulticastDownload")]
    public class StartMulticastDownloadCommand : MulticastCmdlet, IMulticastSettings
    {
        private IFolder rootFolder = FileSystem.Current.LocalStorage;
        private string destinationPath;
        private IEncoderFactory encoderFactory;
        private string passPhrase;
        private string publicKey;

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
        /// Gets or sets a value indicating whether to display UI status during the download.
        /// <para type="description">Whether to display UI status during the download.</para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if displaying UI status; otherwise, <c>false</c>.
        /// </value>
        [Parameter]
        public bool DisplayUiStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>
        /// A task object.
        /// </returns>
        protected override async Task Run()
        {
            this.rootFolder = await FileSystem.Current.GetFolderFromPathAsync(this.destinationPath);
            if (this.rootFolder == null)
            {
                IFolder curDir = await FileSystem.Current.GetFolderFromPathAsync(Environment.CurrentDirectory);
                this.rootFolder = await curDir.CreateFolderAsync(this.destinationPath, CreationCollisionOption.ReplaceExisting);
            }

            if (!string.IsNullOrEmpty(this.passPhrase))
            {
                this.encoderFactory = new PassphraseEncoderFactory(this.passPhrase, Encoding.Unicode);
            }

            if (!string.IsNullOrEmpty(this.publicKey))
            {
                this.encoderFactory = await AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, this.publicKey, AsymmetricSecretFlags.None);
            }

            using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(this.Uri, this))
            {
                Task transferTask = client.StartTransfer(this.Token);
                if (this.DisplayUiStatus)
                {
                    using (StatusViewer viewer = new StatusViewer(client, client, client, this.UpdateInterval))
                    {
                        Application.Run(viewer);
                    }
                }
                else
                {
                    while (!transferTask.IsCompleted && !transferTask.IsCanceled && !transferTask.IsFaulted)
                    {
                        Thread.Sleep(this.UpdateInterval);
                        this.WriteTransferProgress(0, client);
                        this.WriteTransferReception(0, client);
                    }

                    this.WriteTransferProgressComplete(0, client);
                    this.WriteTransferReceptionComplete(0, client);
                }

                await transferTask;
                await client.Close();
            }
        }
    }
}
