// <copyright file="UriParametersTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using Core;
    using Xunit;

    public class UriParametersTest
    {
        [Theory]
        [InlineData("mc://foo/test", "foo", 0, false, "/test")]
        [InlineData("mcs://foo/test", "foo", 0, true, "/test")]
        [InlineData("mcs://bar.baz:123/test/file.cab", "bar.baz", 123, true, "/test/file.cab")]
        [InlineData("mcs://bar.baz:123/test\\file.cab", "bar.baz", 123, true, "/test/file.cab")]
        public void TestCtor(string uri, string hostname, int port, bool useTLs, string path)
        {
            UriParameters p = new UriParameters(new Uri(uri));
            Assert.Equal(p.Hostname, hostname);
            Assert.Equal(p.Port, port == 0 ? UriParameters.DefaultPort : port);
            Assert.Equal(p.UseTls, useTLs);
            Assert.Equal(p.Path, path);
        }

        [Theory]
        [InlineData("foo", 0, null, "test", "mc://foo/test")]
        [InlineData("foo", 0, true, "test", "mcs://foo/test")]
        [InlineData("bar.baz", 123, true, "test/file.cab", "mcs://bar.baz:123/test/file.cab")]
        [InlineData("bar.baz", 123, true, "test\\file.cab", "mcs://bar.baz:123/test\\file.cab")]
        public void TestToUdp(string hostname, int port, bool? useTls, string path, string actualUri)
        {
            UriParameters p = new UriParameters();
            if (!string.IsNullOrEmpty(hostname))
            {
                p.Hostname = hostname;
            }

            if (port > 0)
            {
                p.Port = port;
            }

            if (!string.IsNullOrEmpty(path))
            {
                p.Path = path;
            }

            if (useTls != null)
            {
                p.UseTls = useTls.Value;
            }

            Uri expected = new Uri(actualUri);
            Uri actual = p.ToUri();
            Assert.Equal(expected, actual);
            Assert.Equal(expected.ToString(), actual.ToString());
        }
    }
}
