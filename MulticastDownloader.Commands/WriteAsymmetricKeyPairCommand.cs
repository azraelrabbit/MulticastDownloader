// <copyright file="WriteAsymmetricKeyPairCommand.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System.IO;
    using System.Management.Automation;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core.Cryptography;
    using PCLStorage;

    /// <summary>
    /// <para type="synopsis">Writes an asymmetric key pair.</para>
    /// <para type="description">This writes an asymmetric key pair to the specified files. The key pair can be used as a parameter to either the <see cref="StartMulticastDownloadCommand"/> or <see cref="StartMulticastServerCommand"/>.</para>
    /// </summary>
    /// <seealso cref="Cmdlet" />
    /// <seealso cref="SecretWriter"/>
    [Cmdlet(VerbsData.Save, "AsymmetricKeyPair")]
    public class WriteAsymmetricKeyPairCommand : AsyncCmdlet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteAsymmetricKeyPairCommand"/> class.
        /// </summary>
        public WriteAsymmetricKeyPairCommand()
        {
            this.Strength = 2048;
            this.PublicKey = "id_pub.rsa";
            this.PrivateKey = "id_priv.rsa";
            this.LogLevel = LogLevel.Off;
        }

        /// <summary>
        /// Gets or sets the strength in bits.
        /// <para type="description">The strength in bits.</para>
        /// </summary>
        /// <value>
        /// The strength.
        /// </value>
        [Parameter]
        public int Strength
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the public key file.
        /// <para type="description">The public key file.</para>
        /// </summary>
        /// <value>
        /// The public key.
        /// </value>
        [Parameter(HelpMessage = "The public key file", Mandatory = true)]
        public string PublicKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the private key file.
        /// <para type="description">The private key file.</para>
        /// </summary>
        /// <value>
        /// The private key.
        /// </value>
        [Parameter(HelpMessage = "The private key file", Mandatory = true)]
        public string PrivateKey
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
            await SecretWriter.WriteAsymmetricKeyPair(FileSystem.Current.LocalStorage, this.PrivateKey, this.PublicKey, this.Strength);
        }
    }
}
