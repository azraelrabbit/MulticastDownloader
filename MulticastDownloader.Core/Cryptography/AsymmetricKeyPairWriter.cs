// <copyright file="AsymmetricKeyPairWriter.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Security;
    using PCLStorage;

    /// <summary>
    /// Represent an asymmetric key pair generator.
    /// </summary>
    public static class AsymmetricKeyPairWriter
    {
        /// <summary>
        /// Writes the PEM-encoded asymmetric key pair cipher to a pair of streams.
        /// </summary>
        /// <param name="privateKeyFile">The private key file.</param>
        /// <param name="publicKeyFile">The public key file.</param>
        /// <param name="strength">The strength.</param>
        public static void WriteAsymmetricKeyPair(Stream privateKeyFile, Stream publicKeyFile, int strength)
        {
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), strength));
            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            PemWriter privateWriter = new PemWriter(new StreamWriter(privateKeyFile));
            privateWriter.WriteObject(keyPair.Private);
            PemWriter publicWriter = new PemWriter(new StreamWriter(publicKeyFile));
            publicWriter.WriteObject(keyPair.Public);
            privateWriter.Writer.Flush();
            publicWriter.Writer.Flush();
        }

        /// <summary>
        /// Writes the PEM-encoded asymmetric key pair cipher to a pair of streams.
        /// </summary>
        /// <param name="privateKeyFile">The private key file.</param>
        /// <param name="publicKeyFile">The public key file.</param>
        /// <param name="strength">The strength.</param>
        /// <returns>A task object.</returns>
        public static async Task WriteAsymmetricKeyPair(string privateKeyFile, string publicKeyFile, int strength)
        {
            IFile privateFile = await FileSystem.Current.LocalStorage.CreateFileAsync(privateKeyFile, CreationCollisionOption.ReplaceExisting);
            IFile publicFile = await FileSystem.Current.LocalStorage.CreateFileAsync(publicKeyFile, CreationCollisionOption.ReplaceExisting);
            using (Stream privateStream = await privateFile.OpenAsync(FileAccess.ReadAndWrite))
            using (Stream publicStream = await publicFile.OpenAsync(FileAccess.ReadAndWrite))
            {
                WriteAsymmetricKeyPair(privateStream, publicStream, strength);
            }
        }
    }
}
