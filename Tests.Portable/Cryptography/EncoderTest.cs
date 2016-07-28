// <copyright file="EncoderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using PCLStorage;
    using Xunit;
    using Crypto = Core.Cryptography;

    public class EncoderTest
    {
        [Theory]
        [InlineData("123")]
        public void PassphraseEncoderConstructed(string passPhrase)
        {
            Crypto.PassphraseEncoderFactory fac = new Crypto.PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            Crypto.PassphraseEncoder enc = (Crypto.PassphraseEncoder)fac.CreateEncoder();
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048)]
        public async Task AsymmetricKeyFilesCanBeGenerated(string privateKey, string publicKey, int strength)
        {
            Assert.NotEmpty(privateKey);
            Assert.NotEmpty(publicKey);
            Assert.NotInRange(strength, int.MinValue, 0);
            await Crypto.SecretWriter.WriteAsymmetricKeyPair(FileSystem.Current.LocalStorage, privateKey, publicKey, strength);
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048)]
        public async Task EncoderConstructsWithPublicKey(string publicKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Assert.NotNull(encodedPublic1);
        }

        [Theory]
        [InlineData("TestPrivate.pem", 2048)]
        public async Task EncoderConstructsWithPrivateKey(string privateKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoderFactory encodedPrivate1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            Assert.NotNull(encodedPrivate1);
        }

        [Theory]
        [InlineData("12345", new int[] { 1, 2, 3, 4, 5, 6, 7, 8, -1, 9, 10, 11, 12 })]
        public void PassphraseEncoderDecodesPassphraseData(string passPhrase, int[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(passPhrase);
            Crypto.PassphraseEncoderFactory enc = new Crypto.PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            Crypto.PassphraseEncoderFactory dec = new Crypto.PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            CheckDataEquals(testData, enc.CreateEncoder(), dec.CreateDecoder());
        }

        [Theory]
        [InlineData("12345", "456", new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public void PassphraseEncoderDoesNotDecodeWithWrongPassphrase(string expectedPhrase, string actualPhrase, int[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(expectedPhrase);
            Assert.NotEmpty(actualPhrase);
            Assert.NotEqual(expectedPhrase, actualPhrase);
            Crypto.PassphraseEncoderFactory enc = new Crypto.PassphraseEncoderFactory(expectedPhrase, Encoding.UTF8);
            Crypto.PassphraseEncoderFactory dec = new Crypto.PassphraseEncoderFactory(actualPhrase, Encoding.UTF8);
            CheckDataNotEquals(testData, enc.CreateEncoder(), dec.CreateDecoder());
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDecodesPrivateKeyData(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoderFactory encodedPrivate1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataEquals(testData, encodedPrivate1.CreateEncoder(), encodedPublic1.CreateDecoder());
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PrivateEncoderDecodesPublicKeyData(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoderFactory encodedPrivate1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataEquals(testData, encodedPublic1.CreateEncoder(), encodedPrivate1.CreateDecoder());
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDoesNotDecodePublicKeyData(string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoderFactory encodedPublic2 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            CheckDataNotEquals(testData, encodedPublic1.CreateEncoder(), encodedPublic2.CreateDecoder());
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicDecoderDoesNotDecodeAnotherPrivateEncoder(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoderFactory encodedPrivate1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataNotEquals(testData, encodedPrivate1.CreateEncoder(), encodedPublic1.CreateDecoder());
        }

        [Theory]
        [InlineData("12345", 128, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public void PassphraseEncoderParalellizable(string passPhrase, int numThreads, int[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(passPhrase);
            Crypto.PassphraseEncoderFactory enc = new Crypto.PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            Crypto.PassphraseEncoderFactory dec = new Crypto.PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            Parallel.For(0, numThreads, (d, s) =>
            {
                CheckDataEquals(testData, enc.CreateEncoder(), dec.CreateDecoder());
            });
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, 10, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task AsymmetricEncoderParalellizable(string privateKey, string publicKey, int strength, int numThreads, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoderFactory encodedPublic1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoderFactory encodedPrivate1 = await Crypto.AsymmetricEncoderFactory.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            Parallel.For(0, numThreads, (d, s) =>
            {
                CheckDataEquals(testData, encodedPrivate1.CreateEncoder(), encodedPublic1.CreateDecoder());
            });
        }

        private static void CheckDataNotEquals(int[] testData, Crypto.IEncoder enc, Crypto.IDecoder dec)
        {
            List<byte> buf = new List<byte>();
            foreach (int v in testData)
            {
                if (v < 0)
                {
                    byte[] data = buf.ToArray();
                    int encodedLength = enc.GetEncodedOutputLength(testData.Length);
                    byte[] encoded = enc.Encode(data);
                    Assert.Equal(encoded.Length, encodedLength);
                    byte[] decoded = null;
                    try
                    {
                        decoded = dec.Decode(encoded);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.NotEqual(data, decoded);
                }
                else
                {
                    buf.Add((byte)v);
                }
            }
        }

        private static void CheckDataEquals(int[] testData, Crypto.IEncoder enc, Crypto.IDecoder dec)
        {
            List<byte> buf = new List<byte>();
            foreach (int v in testData)
            {
                if (v < 0)
                {
                    byte[] data = buf.ToArray();
                    int encodedLength = enc.GetEncodedOutputLength(testData.Length);
                    byte[] encoded = enc.Encode(data);
                    Assert.Equal(encoded.Length, encodedLength);
                    byte[] decoded = dec.Decode(encoded);
                    Assert.Equal(data.Length, decoded.Length);
                    Assert.Equal(data, decoded);
                }
                else
                {
                    buf.Add((byte)v);
                }
            }
        }
    }
}
