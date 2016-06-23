// <copyright file="SecretTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Cryptography
{
    using System;
    using System.Text;
    using MS.MulticastDownloader.Core.Cryptography;
    using Xunit;

    public class SecretTest
    {
        [Fact]
        public void Core_Cryptography_SecretTest_TestPassPhraseSecret()
        {
            PassphraseSecret secret = new PassphraseSecret("foo", Encoding.UTF8);
            byte[] encoded1 = secret.CreateKey(1);
            byte[] encoded2 = secret.CreateKey(3);
            Assert.Equal(encoded1.Length, encoded2.Length);
            Assert.Equal(encoded1, encoded2);
            string decoded1 = Encoding.UTF8.GetString(encoded1, 0, encoded2.Length);
            Assert.Equal("foo", decoded1);
            byte[] encoded3 = secret.CreateKey(6);
            string decoded3 = Encoding.UTF8.GetString(encoded3, 0, encoded3.Length);
            Assert.Equal("foo\0\0\0", decoded3);
        }
    }
}
