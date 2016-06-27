// <copyright file="AsymmetricEncoder.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Engines;
    using Org.BouncyCastle.Crypto.Paddings;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.OpenSsl;
    using PCLStorage;
    using Properties;

    /// <summary>
    /// Represent a key-based asymmetric encoder using PEM format certificates for storing key data.
    /// </summary>
    /// <seealso cref="MS.MulticastDownloader.Core.Cryptography.IEncoder" />
    public class AsymmetricEncoder : IEncoder
    {
        private BufferedAsymmetricBlockCipher encoder;
        private BufferedAsymmetricBlockCipher decoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsymmetricEncoder"/> class.
        /// </summary>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The flags.</param>
        public AsymmetricEncoder(Stream certData, AsymmetricSecretFlags flags)
        {
            this.Init(certData, flags, null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsymmetricEncoder"/> class.
        /// </summary>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The cert flags.</param>
        /// <param name="passwordFinder">The password finder.</param>
        public AsymmetricEncoder(Stream certData, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            this.Init(certData, flags, passwordFinder);
        }

        /// <summary>
        /// Loads the specified cert data.
        /// </summary>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The flags.</param>
        /// <returns>An <see cref="AsymmetricEncoder"/>.</returns>
        public static async Task<AsymmetricEncoder> Load(string certData, AsymmetricSecretFlags flags)
        {
            return await Load(certData, flags, null);
        }

        /// <summary>
        /// Loads the specified cert data.
        /// </summary>
        /// <param name="certData">The cert data.</param>
        /// <param name="flags">The cert flags.</param>
        /// <param name="passwordFinder">The password finder.</param>
        /// <returns>An <see cref="AsymmetricEncoder"/>.</returns>
        public static async Task<AsymmetricEncoder> Load(string certData, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            IFile keyFile = await FileSystem.Current.LocalStorage.GetFileAsync(certData);
            using (Stream certStream = await keyFile.OpenAsync(FileAccess.Read))
            {
                AsymmetricEncoder ret = new AsymmetricEncoder(certStream, flags, passwordFinder);
                return ret;
            }
        }

        /// <summary>
        /// Gets the length of the encoded output.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The length in bytes.</returns>
        public int GetEncodedOutputLength(int input)
        {
            return this.encoder.GetOutputSize(input);
        }

        /// <summary>
        /// Encodes the specified unencoded.
        /// </summary>
        /// <param name="unencoded">The unencoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Encode(byte[] unencoded)
        {
            if (unencoded == null)
            {
                throw new ArgumentNullException("unencoded");
            }

            return this.encoder.Process(unencoded);
        }

        /// <summary>
        /// Decodes the specified encoded.
        /// </summary>
        /// <param name="encoded">The encoded data.</param>
        /// <returns>A byte array.</returns>
        public byte[] Decode(byte[] encoded)
        {
            if (encoded == null)
            {
                throw new ArgumentNullException("encoded");
            }

            return this.decoder.Process(encoded);
        }

        private void Init(Stream certData, AsymmetricSecretFlags flags, IPasswordFinder passwordFinder)
        {
            PemReader reader;
            if (passwordFinder == null)
            {
                reader = new PemReader(new StreamReader(certData));
            }
            else
            {
                reader = new PemReader(new StreamReader(certData), passwordFinder);
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

            this.encoder = new BufferedAsymmetricBlockCipher(new RsaEngine());
            this.decoder = new BufferedAsymmetricBlockCipher(new RsaEngine());
            this.encoder.Init(true, keyParam);
            this.decoder.Init(false, keyParam);
        }
    }
}
