// <copyright file="EncoderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using PCLStorage;
    using Xunit;
    using Crypto = Core.Cryptography;

    public class EncoderTest
    {
        [Theory]
        [InlineData("123", 128)]
        public void PassphraseEncoderConstructed(string passPhrase, int strength)
        {
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(passPhrase,  Encoding.UTF8, strength);
            Assert.Equal(passPhrase, enc.Passphrase);
            Assert.Equal(strength, enc.Strength);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048)]
        public async Task AsymmetricKeyFilesCanBeGenerated(string privateKey, string publicKey, int strength)
        {
            Assert.NotEmpty(privateKey);
            Assert.NotEmpty(publicKey);
            Assert.NotInRange(strength, int.MinValue, 0);
            await Crypto.AsymmetricKeyPairWriter.WriteAsymmetricKeyPair(FileSystem.Current.LocalStorage, privateKey, publicKey, strength);
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048)]
        public async Task EncoderConstructsWithPublicKey(string publicKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Assert.NotNull(encodedPublic1);
        }

        [Theory]
        [InlineData("TestPrivate.pem", 2048)]
        public async Task EncoderConstructsWithPrivateKey(string privateKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            Assert.NotNull(encodedPrivate1);
        }

        [Theory]
        [InlineData("12345", 128, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, -1, 9, 10, 11, 12 })]
        public void PassphraseEncoderDecodesPassphraseData(string passPhrase, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(passPhrase);
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(passPhrase, Encoding.UTF8, strength);
            Crypto.PassphraseEncoder dec = new Crypto.PassphraseEncoder(passPhrase, Encoding.UTF8, strength);
            CheckDataEquals(testData, enc, dec);
        }

        [Theory]
        [InlineData("12345", "456", 128, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public void PassphraseEncoderDoesNotDecodeWithWrongPassphrase(string expectedPhrase, string actualPhrase, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(expectedPhrase);
            Assert.NotEmpty(actualPhrase);
            Assert.NotEqual(expectedPhrase, actualPhrase);
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(expectedPhrase, Encoding.UTF8, strength);
            Crypto.PassphraseEncoder dec = new Crypto.PassphraseEncoder(actualPhrase, Encoding.UTF8, strength);
            CheckDataNotEquals(testData, enc, dec);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDecodesPrivateKeyData(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataEquals(testData, encodedPrivate1, encodedPublic1);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PrivateEncoderDecodesPublicKeyData(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataEquals(testData, encodedPublic1, encodedPrivate1);
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDoesNotDecodePublicKeyData(string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPublic2 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            CheckDataNotEquals(testData, encodedPublic1, encodedPublic2);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new int[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicDecoderDoesNotDecodeAnotherPrivateEncoder(string privateKey, string publicKey, int strength, int[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(FileSystem.Current.LocalStorage, privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            CheckDataNotEquals(testData, encodedPrivate1, encodedPublic1);
        }

        private static void CheckDataNotEquals(int[] testData, Crypto.IEncoder enc, Crypto.IEncoder dec)
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

        private static void CheckDataEquals(int[] testData, Crypto.IEncoder enc, Crypto.IEncoder dec)
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
