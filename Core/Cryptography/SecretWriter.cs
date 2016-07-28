// <copyright file="SecretWriter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using IO;
    using Org.BouncyCastle.Asn1;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Operators;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.X509;
    using Org.BouncyCastle.X509.Extension;
    using PCLStorage;
    using asn1x509 = Org.BouncyCastle.Asn1.X509;

    /// <summary>
    /// Represent a stream writer for secrets.
    /// </summary>
    public static class SecretWriter
    {
        /// <summary>
        /// Writes the PEM-encoded asymmetric key pair cipher to a pair of streams.
        /// </summary>
        /// <param name="privateKeyFile">The private key file.</param>
        /// <param name="publicKeyFile">The public key file.</param>
        /// <param name="strength">The strength.</param>
        public static void WriteAsymmetricKeyPair(Stream privateKeyFile, Stream publicKeyFile, int strength)
        {
            AsymmetricCipherKeyPair keyPair = GenerateKeyPair(strength);
            using (StreamWriter privateStreamWriter = new StreamWriter(privateKeyFile))
            using (StreamWriter publicStreamWriter = new StreamWriter(publicKeyFile))
            {
                PemWriter privateWriter = new PemWriter(privateStreamWriter);
                privateWriter.WriteObject(keyPair.Private);
                PemWriter publicWriter = new PemWriter(publicStreamWriter);
                publicWriter.WriteObject(keyPair.Public);
            }
        }

        /// <summary>
        /// Writes the PEM-encoded asymmetric key pair cipher to a pair of streams.
        /// </summary>
        /// <param name="folder">The certificate folder.</param>
        /// <param name="privateKeyFile">The private key file.</param>
        /// <param name="publicKeyFile">The public key file.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>A task object.</returns>
        public static async Task WriteAsymmetricKeyPair(IFolder folder, string privateKeyFile, string publicKeyFile, int strength)
        {
            IFile privateFile = await folder.Create(privateKeyFile, false);
            IFile publicFile = await folder.Create(publicKeyFile, false);
            using (Stream privateStream = await privateFile.OpenAsync(FileAccess.ReadAndWrite))
            using (Stream publicStream = await publicFile.OpenAsync(FileAccess.ReadAndWrite))
            {
                WriteAsymmetricKeyPair(privateStream, publicStream, strength);
            }
        }

        private static AsymmetricCipherKeyPair GenerateKeyPair(int strength)
        {
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), strength));
            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            return keyPair;
        }
    }
}
