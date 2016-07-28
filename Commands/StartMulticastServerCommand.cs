// <copyright file="StartMulticastServerCommand.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Logging.Simple;
    using Core;
    using Core.Cryptography;
    using Core.Server;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// <para type="synopsis">Starts a multicast server.</para>
    /// <para type="description">This starts a multicast server using the specified parameters. This call will block until the user interrupts it.</para>
    /// </summary>
    /// <seealso cref="Cmdlet" />
    /// <seealso cref="MulticastServer" />
    [Cmdlet(VerbsLifecycle.Start, "MulticastServer")]
    public class StartMulticastServerCommand : MulticastCmdlet, IMulticastSettings, IMulticastServerSettings
    {
        private ILog log = LogManager.GetLogger<StartMulticastServerCommand>();
        private IFolder rootFolder = FileSystem.Current.LocalStorage;
        private string sourcePath;
        private IEncoderFactory encoderFactory;
        private string passPhrase;
        private string privateKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartMulticastServerCommand"/> class.
        /// </summary>
        public StartMulticastServerCommand()
        {
            this.DelayCalculation = DelayCalculation.MinimumThroughput;
            this.MaxBytesPerSecond = long.MaxValue;
            this.MaxConnections = 10;
            this.MaxSessions = 10;
            this.Mtu = 1500;
            this.MulticastAddress = "239.0.0.1";
            this.MulticastStartPort = 0xFF00;
        }

        /// <summary>
        /// Gets or sets the delay calculation used to compute the burst delay.
        /// <para type="description">The delay calculation used to compute the burst delay.</para>
        /// </summary>
        /// <value>
        /// The delay calculation.
        /// </value>
        [Parameter]
        public DelayCalculation DelayCalculation
        {
            get;
            set;
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
                this.privateKey = null;
            }
        }

        /// <summary>
        /// Gets or sets the private key used for encoding.
        /// <para type="description">The private key used for encoding.</para>
        /// </summary>
        /// <value>
        /// The public key.
        /// </value>
        [Parameter(ParameterSetName = "PrivateKey")]
        public string PublicKey
        {
            get
            {
                return this.privateKey;
            }

            set
            {
                this.privateKey = value;
                this.passPhrase = null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the interface being used to listen for session requests and send data.
        /// <para type="description">The name of the interface being used to listen for session requests and send data.</para>
        /// </summary>
        /// <value>
        /// The name of the interface.
        /// </value>
        /// <remarks>
        /// A parameter of null can be used to indicate the client can listen for requests on any interface.
        /// However, this isn't recommended.
        /// </remarks>
        [Parameter]
        public string InterfaceName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MulticastAddress" /> parameter specifies an IPV6 address.
        /// <para type="description">Whether the <see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MulticastAddress" /> parameter specifies an IPV6 address.</para>
        /// </summary>
        /// <value>
        /// <c>true</c> if ipv6; otherwise, <c>false</c>.
        /// </value>
        [Parameter]
        public bool Ipv6
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum bytes per second for the multicast download.
        /// <para type="description">The maximum bytes per second for the multicast download.</para>
        /// </summary>
        /// <value>
        /// The maximum bytes per second.
        /// </value>
        [Parameter]
        public long MaxBytesPerSecond
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum number of connections.
        /// <para type="description">The maximum number of connections.</para>
        /// </summary>
        /// <value>
        /// The maximum number of connections.
        /// </value>
        [Parameter]
        public int MaxConnections
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum sessions.
        /// <para type="description">The maximum sessions.</para>
        /// </summary>
        /// <value>
        /// The maximum sessions.
        /// </value>
        /// <remarks>
        /// As multicast sessions are created, they must reserve a multicast port. The number of ports which may be reserved is
        /// in the range [<see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MulticastStartPort" />,<see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MulticastStartPort" /> + <see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MaxSessions" />).
        /// </remarks>
        [Parameter]
        public int MaxSessions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the MTU (maximum transmission unit) for the network. Used for determining the size of multicast data.
        /// <para type="description">The MTU (maximum transmission unit) for the network. Used for determining the size of multicast data.</para>
        /// </summary>
        /// <value>
        /// The MTU.
        /// </value>
        /// <remarks>
        /// The default MTU for internet connections is 576, however local connections should use a value
        /// of 1500 or greater. Check your network settings to determine what the optional MTU is for your network.
        /// </remarks>
        [Parameter]
        public int Mtu
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the multicast address, which is used to broadcast messages from.
        /// <para type="description">The multicast address, which is used to broadcast messages from.</para>
        /// </summary>
        /// <value>
        /// The multicast address.
        /// </value>
        /// <remarks>
        /// This must be a broadcast (IPV4) or multicast (IPV6) address. See RFC 919 or RFC 4291 for more details.
        /// </remarks>
        [Parameter]
        public string MulticastAddress
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the multicast start port.
        /// <para type="description">The multicast start port.</para>
        /// </summary>
        /// <value>
        /// The multicast start port.
        /// </value>
        /// <remarks>
        /// The multicast start port determines the starting port in the range of ports used to send data to individual multicast sessions.
        /// The <see cref="P:MS.MulticastDownloader.Core.Server.IMulticastServerSettings.MaxSessions" /> parameter determines the port range.
        /// </remarks>
        [Parameter]
        public int MulticastStartPort
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the length of a multicast burst in bytes.
        /// <para type="description">The length of a multicast burst in bytes.</para>
        /// </summary>
        /// <value>
        /// The length of the multicast burst.
        /// </value>
        /// <remarks>
        /// This value is largely system dependent. A burst length which is too large can lead to excessive packet loss during a multicast download.
        /// Ideally a burst length should be large enough that the interval between bursts does not cause clients to become starved for data.
        /// </remarks>
        [Parameter]
        public int MulticastBurstLength
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
        /// Gets or sets the root folder from which the multicast URI will be copied.
        /// <para type="description">The root folder from which the multicast URI will be copied.</para>
        /// </summary>
        /// <value>
        /// The root.
        /// </value>
        [Parameter(Mandatory = true, HelpMessage = "The source path")]
        public string SourcePath
        {
            get
            {
                return this.sourcePath;
            }

            set
            {
                this.sourcePath = value;
            }
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>A task object.</returns>
        protected override async Task Run()
        {
            this.rootFolder = await FileSystem.Current.GetFolderFromPathAsync(this.sourcePath);
            if (this.rootFolder == null)
            {
                throw new FileNotFoundException(Resources.SourcePathNotFound, this.sourcePath);
            }

            if (!string.IsNullOrEmpty(this.passPhrase))
            {
                this.encoderFactory = new PassphraseEncoderFactory(this.passPhrase, Encoding.Unicode);
            }

            if (!string.IsNullOrEmpty(this.privateKey))
            {
                this.encoderFactory = await AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, this.privateKey, AsymmetricSecretFlags.ReadPrivateKey);
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

                uint timerRet = NativeMethods.timeBeginPeriod(1);
                if (timerRet != 0)
                {
                    this.log.Warn("NativeMethods.timeEndPeriod(1) ret=" + timerRet);
                }

                using (MulticastServer server = new MulticastServer(this.Uri, this, this))
                {
                    Task hostTask = server.Listen(cts.Token);
                    while (!hostTask.IsCompleted && !hostTask.IsCanceled && !hostTask.IsFaulted)
                    {
                        Thread.Sleep(this.UpdateInterval);
                        this.WriteTransferProgress(0, server);
                        this.WriteTransferReception(0, server);
                        foreach (MulticastSession session in server.Sessions)
                        {
                            this.WriteTransferProgress(1 + session.SessionId, session);
                        }
                    }

                    this.WriteTransferProgressComplete(0, server);
                    this.WriteTransferReceptionComplete(0, server);
                    foreach (MulticastSession session in server.Sessions)
                    {
                        this.WriteTransferProgressComplete(1 + session.SessionId, session);
                    }

                    await hostTask;
                }

                timerRet = NativeMethods.timeEndPeriod(1);
                if (timerRet != 0)
                {
                    this.log.Warn("NativeMethods.timeEndPeriod(1) ret=" + timerRet);
                }
            }
        }

        private static class NativeMethods
        {
            [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
            internal static extern uint timeBeginPeriod(uint uMilliseconds);

            [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
            internal static extern uint timeEndPeriod(uint uMilliseconds);
        }
    }
}
