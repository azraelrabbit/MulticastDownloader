// <copyright file="EncoderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Util;
    using Xunit;
    using Crypto = Core.Cryptography;
    using PS = PCLStorage;

    public class EncoderTest
    {
        [Theory]
        [InlineData("123", 128)]
        public void PassphraseEncoderConstructed(string passPhrase, int strength)
        {
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(passPhrase,  Encoding.UTF8, strength);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048)]
        public async Task AsymmetricKeyFilesCanBeGenerated(string privateKey, string publicKey, int strength)
        {
            Assert.NotEmpty(privateKey);
            Assert.NotEmpty(publicKey);
            Assert.NotInRange(strength, int.MinValue, 0);
            await Crypto.AsymmetricKeyPairWriter.WriteAsymmetricKeyPair(privateKey, publicKey, strength);
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048)]
        public async Task EncoderConstructsWithPublicKey(string publicKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            Assert.NotNull(encodedPublic1);
        }

        [Theory]
        [InlineData("TestPrivate.pem", 2048)]
        public async Task EncoderConstructsWithPrivateKey(string privateKey, int strength)
        {
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            Assert.NotNull(encodedPrivate1);
        }

        [Theory]
        [InlineData("12345", 128, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public void PassphraseEncoderDecodesPassphraseData(string passPhrase, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(passPhrase);
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(passPhrase, Encoding.UTF8, strength);
            Crypto.PassphraseEncoder dec = new Crypto.PassphraseEncoder(passPhrase, Encoding.UTF8, strength);
            int encodedLength = enc.GetEncodedOutputLength(testData.Length);
            byte[] encoded = enc.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = dec.Decode(encoded);
            Assert.Equal(testData.Length, decoded.Length);
            Assert.Equal(testData, decoded);
        }

        [Theory]
        [InlineData("12345", "456", 128, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public void PassphraseEncoderDoesNotDecodeWithWrongPassphrase(string expectedPhrase, string actualPhrase, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            Assert.NotEmpty(expectedPhrase);
            Assert.NotEmpty(actualPhrase);
            Assert.NotEqual(expectedPhrase, actualPhrase);
            Crypto.PassphraseEncoder enc = new Crypto.PassphraseEncoder(expectedPhrase, Encoding.UTF8, strength);
            Crypto.PassphraseEncoder dec = new Crypto.PassphraseEncoder(actualPhrase, Encoding.UTF8, strength);
            int encodedLength = enc.GetEncodedOutputLength(testData.Length);
            byte[] encoded = enc.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = null;
            try
            {
                decoded = dec.Decode(encoded);
            }
            catch (Exception)
            {
            }

            Assert.NotEqual(testData, decoded);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDecodesPrivateKeyData(string privateKey, string publicKey, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            int encodedLength = encodedPrivate1.GetEncodedOutputLength(testData.Length);
            byte[] encoded = encodedPrivate1.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = encodedPublic1.Decode(encoded);
            Assert.Equal(testData.Length, decoded.Length);
            Assert.Equal(testData, decoded);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PrivateEncoderDecodesPublicKeyData(string privateKey, string publicKey, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            int encodedLength = encodedPublic1.GetEncodedOutputLength(testData.Length);
            byte[] encoded = encodedPublic1.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = encodedPrivate1.Decode(encoded);
            Assert.Equal(testData.Length, decoded.Length);
            Assert.Equal(testData, decoded);
        }

        [Theory]
        [InlineData("TestPublic.pem", 2048, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicEncoderDoesNotDecodePublicKeyData(string publicKey, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPublic2 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            int encodedLength = encodedPublic1.GetEncodedOutputLength(testData.Length);
            byte[] encoded = encodedPublic1.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = null;
            try
            {
                decoded = encodedPublic2.Decode(encoded);
            }
            catch (Exception)
            {
            }

            Assert.NotEqual(testData, decoded);
        }

        [Theory]
        [InlineData("TestPrivate.pem", "TestPublic.pem", 2048, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })]
        public async Task PublicDecoderDoesNotDecodeAnotherPrivateEncoder(string privateKey, string publicKey, int strength, byte[] testData)
        {
            Assert.NotNull(testData);
            await this.AsymmetricKeyFilesCanBeGenerated("unused", publicKey, strength);
            await this.AsymmetricKeyFilesCanBeGenerated(privateKey, "unused", strength);
            Crypto.AsymmetricEncoder encodedPublic1 = await Crypto.AsymmetricEncoder.Load(publicKey, Crypto.AsymmetricSecretFlags.None);
            Crypto.AsymmetricEncoder encodedPrivate1 = await Crypto.AsymmetricEncoder.Load(privateKey, Crypto.AsymmetricSecretFlags.ReadPrivateKey);
            int encodedLength = encodedPrivate1.GetEncodedOutputLength(testData.Length);
            byte[] encoded = encodedPrivate1.Encode(testData);
            Assert.Equal(encoded.Length, encodedLength);
            byte[] decoded = null;
            try
            {
                decoded = encodedPublic1.Decode(encoded);
            }
            catch (Exception)
            {
            }

            Assert.NotEqual(testData, decoded);
        }
    }
}
