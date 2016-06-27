// <copyright file="EncoderTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System.IO;
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
        [Fact]
        public void EncoderConstructedWithPassphraseKey()
        {
            Crypto.Encoder enc = new Crypto.Encoder(new Crypto.PassphraseSecret("123", Encoding.UTF8));
        }

        [Fact]
        public async Task EncoderConstructsWithPublicKey()
        {
            if (!Runtime.HasFileSystem())
            {
                return;
            }

            PS.IFile publicKey = await PS.FileSystem.Current.GetFileFromPathAsync("TestKey.cer");
            using (Stream s = await publicKey.OpenAsync(PS.FileAccess.Read))
            {
                Crypto.CertificateSecret encodedPublic1 = new Crypto.CertificateSecret(s, Crypto.CertificateFlags.Validate);
                ICipherParameters encoded1 = encodedPublic1.CreateCipher(123);
                s.Position = 0;
                Crypto.CertificateSecret encodedPublic2 = new Crypto.CertificateSecret(s, Crypto.CertificateFlags.None);
                ICipherParameters encoded2 = encodedPublic2.CreateCipher(456);
            }
        }

        [Fact]
        public async Task EncoderConstructsWithPrivateKey()
        {
            if (!Runtime.HasFileSystem())
            {
                return;
            }

            PS.IFile privateKey = await PS.FileSystem.Current.GetFileFromPathAsync("TestKeyPrivate.der");
            using (Stream s = await privateKey.OpenAsync(PS.FileAccess.Read))
            {
                Crypto.CertificateSecret encodedPrivate1 = new Crypto.CertificateSecret(s, /* CertificateFlags.ReadPrivateKey | */ Crypto.CertificateFlags.Validate);
                ICipherParameters encoded1 = encodedPrivate1.CreateCipher(123);
                s.Position = 0;
                Crypto.CertificateSecret encodedPrivate2 = new Crypto.CertificateSecret(s, Crypto.CertificateFlags.None);
                ICipherParameters encoded2 = encodedPrivate2.CreateCipher(456);
            }
        }
    }
}
