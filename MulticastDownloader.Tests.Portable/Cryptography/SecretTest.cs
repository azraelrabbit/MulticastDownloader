// <copyright file="SecretTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Core.Cryptography;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Util;
    using Xunit;
    using PS = PCLStorage;

    public class SecretTest
    {
        [Fact]
        public void CreatesPassPhraseSecret()
        {
            PassphraseSecret secret = new PassphraseSecret("foo", Encoding.UTF8);
            ICipherParameters encoded1 = secret.CreateCipher(1);
            ICipherParameters encoded2 = secret.CreateCipher(3);
            ICipherParameters encoded3 = secret.CreateCipher(6);
        }

        [Fact]
        public async Task CreatesCertificateSecret()
        {
            if (!Runtime.HasFileSystem())
            {
                return;
            }

            PS.IFile publicKey = await PS.FileSystem.Current.GetFileFromPathAsync("TestKey.cer");
            using (Stream s = await publicKey.OpenAsync(PS.FileAccess.Read))
            {
                CertificateSecret encodedPublic1 = new CertificateSecret(s, CertificateFlags.Validate);
                ICipherParameters encoded1 = encodedPublic1.CreateCipher(123);
                s.Position = 0;
                CertificateSecret encodedPublic2 = new CertificateSecret(s, CertificateFlags.None);
                ICipherParameters encoded2 = encodedPublic2.CreateCipher(456);
            }

            PS.IFile privateKey = await PS.FileSystem.Current.GetFileFromPathAsync("TestKeyPrivate.der");
            using (Stream s = await privateKey.OpenAsync(PS.FileAccess.Read))
            {
                CertificateSecret encodedPrivate1 = new CertificateSecret(s, /* CertificateFlags.ReadPrivateKey | */ CertificateFlags.Validate);
                ICipherParameters encoded1 = encodedPrivate1.CreateCipher(123);
                s.Position = 0;
                CertificateSecret encodedPrivate2 = new CertificateSecret(s, CertificateFlags.None);
                ICipherParameters encoded2 = encodedPrivate2.CreateCipher(456);
            }
        }
    }
}
