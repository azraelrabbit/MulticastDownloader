// <copyright file="AsymmetricEncoderFactory.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.OpenSsl;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent an encoder factory for objects of type <see cref="AsymmetricEncoder"/>.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IEncoderFactory" />
    public class AsymmetricEncoderFactory : IEncoderFactory
    {
        private object streamLock = new object();
        private AsymmetricKeyParameter keyParam;
        private AsymmetricSecretFlags flags;
        private IPasswordFinder passwordFinder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsymmetricEncoderFactory"/> class.
        /// </summary>
        /// <param name="keyParam">The key parameter.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="passwordFinder">The password finder.</param>
        public AsymmetricEncoderFactory(AsymmetricKeyParameter keyParam, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            if (keyParam == null)
            {
                throw new ArgumentNullException("certData");
            }

            this.keyParam = keyParam;
            this.flags = flags;
            this.passwordFinder = passwordFinder;
        }

        /// <summary>
        /// Loads the specified cert data.
        /// </summary>
        /// <param name="folder">The cert folder.</param>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>An <see cref="AsymmetricEncoder"/>.</returns>
        public static async Task<AsymmetricEncoderFactory> Load(IFolder folder, string certData, AsymmetricSecretFlags flags)
        {
            return await Load(folder, certData, flags, null);
        }

        /// <summary>
        /// Loads the specified cert data.
        /// </summary>
        /// <param name="folder">The cert folder.</param>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The cert flags.</param>
        /// <param name="passwordFinder">The password finder.</param>
        /// <returns>An <see cref="AsymmetricEncoder"/>.</returns>
        public static async Task<AsymmetricEncoderFactory> Load(IFolder folder, string certData, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            IFile keyFile = await folder.GetFileAsync(certData);
            using (Stream certStream = await keyFile.OpenAsync(FileAccess.Read))
            {
                PemReader reader;
                if (passwordFinder == null)
                {
                    reader = new PemReader(new StreamReader(certStream));
                }
                else
                {
                    reader = new PemReader(new StreamReader(certStream), passwordFinder);
                }

                object obj = reader.ReadObject();
                AsymmetricKeyParameter keyParam;
                if (obj is AsymmetricKeyParameter)
                {
                    keyParam = (AsymmetricKeyParameter)obj;
                }
                else if (obj is AsymmetricCipherKeyPair)
                {
                    AsymmetricCipherKeyPair cipherPair = (AsymmetricCipherKeyPair)obj;
                    if (flags.HasFlag(AsymmetricSecretFlags.ReadPrivateKey))
                    {
                        keyParam = cipherPair.Private;
                    }
                    else
                    {
                        keyParam = cipherPair.Public;
                    }
                }
                else
                {
                    throw new InvalidOperationException(Resources.CertificateTypeMismatch);
                }

                if (flags.HasFlag(AsymmetricSecretFlags.ReadPrivateKey) != keyParam.IsPrivate)
                {
                    throw new InvalidOperationException(Resources.CertificateDoesNotContainPrivateKey);
                }

                AsymmetricEncoderFactory ret = new AsymmetricEncoderFactory(keyParam, flags, passwordFinder);
                return ret;
            }
        }

        /// <summary>
        /// Creates the encoder.
        /// </summary>
        /// <returns>
        /// An encoder.
        /// </returns>
        public IEncoder CreateEncoder()
        {
            return new AsymmetricEncoder(this.keyParam, this.flags, this.passwordFinder, true);
        }

        /// <summary>
        /// Creates the decoder.
        /// </summary>
        /// <returns>
        /// A decoder.
        /// </returns>
        public IDecoder CreateDecoder()
        {
            return new AsymmetricEncoder(this.keyParam, this.flags, this.passwordFinder, false);
        }
    }
}
