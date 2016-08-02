// <copyright file="UdpTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.IO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Logging.Simple;
    using Core;
    using Core.Cryptography;
    using Core.IO;
    using Core.Server;
    using Core.Server.IO;
    using Core.Session;
    using IO;
    using PCLStorage;
    using Xunit;
    using Xunit.Abstractions;

    public class UdpTest
    {
        public UdpTest(ITestOutputHelper outputHelper)
        {
            LogManager.Adapter = new TestOutputLoggerFactoryAdapter(LogLevel.All, outputHelper);
        }

        [Theory]
        [InlineData("mc://localhost:10001", 1 << 20, 1, 10, 1, 576)]
        [InlineData("mc://localhost:10002", 1 << 20, 1, 100, 10, 1500)]
        public async Task UdpReaderAndWriterCanCommunicate(string uri, int bufferSize, int ttl, int numAttempts, int burstSize, int mtu)
        {
            UriParameters parms = new UriParameters(new Uri(uri));
            MulticastSettings settings = new MulticastSettings(null, bufferSize, ttl);
            MulticastServerSettings serverSettings = new MulticastServerSettings(false, mtu);
            IUdpMulticastFactory factory = PortableTestUdpMulticast.CreateFactory(mtu);
            using (UdpReader<FileSegment> reader = new UdpReader<FileSegment>(parms, settings, factory.CreateMulticast()))
            using (UdpWriter writer = new UdpWriter(parms, settings, serverSettings, factory.CreateMulticast()))
            {
                try
                {
                    Random r = new Random();
                    await writer.StartMulticastServer(0, null);
                    await reader.JoinMulticastServer(new SessionJoinResponse { Ipv6 = false, Mtu = mtu, MulticastAddress = serverSettings.MulticastAddress, MulticastBurstLength = burstSize, MulticastPort = serverSettings.MulticastStartPort }, null);
                    await MulticastLoopback(numAttempts, burstSize, mtu, reader, writer, r);
                }
                finally
                {
                    await reader.Close();
                    await writer.Close();
                }
            }
        }

        [Theory]
        [InlineData("mc://localhost:11001", "foo123", 1 << 20, 1, 10, 1, 576)]
        [InlineData("mc://localhost:11002", "bar123", 1 << 20, 1, 100, 10, 1500)]
        public async Task UdpReaderAndWriterCanCommunicateIfEncryptedWithPassPhrase(string uri, string passPhrase, int bufferSize, int ttl, int numAttempts, int burstSize, int mtu)
        {
            PassphraseEncoderFactory passPhraseEncoder = new PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            UriParameters parms = new UriParameters(new Uri(uri));
            MulticastSettings settings = new MulticastSettings(null, bufferSize, ttl);
            MulticastServerSettings serverSettings = new MulticastServerSettings(false, mtu);
            IUdpMulticastFactory factory = PortableTestUdpMulticast.CreateFactory(mtu);
            using (UdpReader<FileSegment> reader = new UdpReader<FileSegment>(parms, settings, factory.CreateMulticast()))
            using (UdpWriter writer = new UdpWriter(parms, settings, serverSettings, factory.CreateMulticast()))
            {
                try
                {
                    Random r = new Random();
                    await writer.StartMulticastServer(0, passPhraseEncoder);
                    await reader.JoinMulticastServer(new SessionJoinResponse { Ipv6 = false, Mtu = mtu, MulticastAddress = serverSettings.MulticastAddress, MulticastBurstLength = burstSize, MulticastPort = serverSettings.MulticastStartPort }, passPhraseEncoder);
                    await MulticastLoopback(numAttempts, burstSize, mtu, reader, writer, r);
                }
                finally
                {
                    await reader.Close();
                    await writer.Close();
                }
            }
        }

        [Theory]
        [InlineData("mc://localhost:11001", "foo123", "bar123", 1 << 20, 1, 10, 1, 576)]
        [InlineData("mc://localhost:11002", "bar123", "foo123", 1 << 20, 1, 100, 10, 1500)]
        public async Task UdpReaderAndWriterCantCommunicateIfPassphraseMismatched(string uri, string passPhrase1, string passPhrase2, int bufferSize, int ttl, int numAttempts, int burstSize, int mtu)
        {
            PassphraseEncoderFactory passPhraseEncoder1 = new PassphraseEncoderFactory(passPhrase1, Encoding.UTF8);
            PassphraseEncoderFactory passPhraseEncoder2 = new PassphraseEncoderFactory(passPhrase2, Encoding.UTF8);
            UriParameters parms = new UriParameters(new Uri(uri));
            MulticastSettings settings = new MulticastSettings(null, bufferSize, ttl);
            MulticastServerSettings serverSettings = new MulticastServerSettings(false, mtu);
            IUdpMulticastFactory factory = PortableTestUdpMulticast.CreateFactory(mtu);
            using (UdpReader<FileSegment> reader = new UdpReader<FileSegment>(parms, settings, factory.CreateMulticast()))
            using (UdpWriter writer = new UdpWriter(parms, settings, serverSettings, factory.CreateMulticast()))
            {
                try
                {
                    Random r = new Random();
                    await writer.StartMulticastServer(0, passPhraseEncoder1);
                    await reader.JoinMulticastServer(new SessionJoinResponse { Ipv6 = false, Mtu = mtu, MulticastAddress = serverSettings.MulticastAddress, MulticastBurstLength = burstSize, MulticastPort = serverSettings.MulticastStartPort }, passPhraseEncoder2);
                    try
                    {
                        await MulticastLoopback(numAttempts, burstSize, mtu, reader, writer, r);
                        Assert.True(false);
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    await reader.Close();
                    await writer.Close();
                }
            }
        }

        [Theory]
        [InlineData("mc://localhost:11001", "foo123", 1 << 20, 1, 10, 1, 576)]
        public async Task UdpReaderAndWriterCantCommunicateIfMissingPassphrase(string uri, string passPhrase1, int bufferSize, int ttl, int numAttempts, int burstSize, int mtu)
        {
            PassphraseEncoderFactory passPhraseEncoder1 = new PassphraseEncoderFactory(passPhrase1, Encoding.UTF8);
            UriParameters parms = new UriParameters(new Uri(uri));
            MulticastSettings settings = new MulticastSettings(null, bufferSize, ttl);
            MulticastServerSettings serverSettings = new MulticastServerSettings(false, mtu);
            IUdpMulticastFactory factory = PortableTestUdpMulticast.CreateFactory(mtu);
            using (UdpReader<FileSegment> reader = new UdpReader<FileSegment>(parms, settings, factory.CreateMulticast()))
            using (UdpWriter writer = new UdpWriter(parms, settings, serverSettings, factory.CreateMulticast()))
            {
                try
                {
                    Random r = new Random();
                    await writer.StartMulticastServer(0, passPhraseEncoder1);
                    await reader.JoinMulticastServer(new SessionJoinResponse { Ipv6 = false, Mtu = mtu, MulticastAddress = serverSettings.MulticastAddress, MulticastBurstLength = burstSize, MulticastPort = serverSettings.MulticastStartPort }, null);
                    try
                    {
                        await MulticastLoopback(numAttempts, burstSize, mtu, reader, writer, r);
                        Assert.True(false);
                    }
                    catch (Exception)
                    {
                    }
                }
                finally
                {
                    await reader.Close();
                    await writer.Close();
                }
            }
        }

        private static async Task MulticastLoopback(int numAttempts, int burstSize, int mtu, UdpReader<FileSegment> reader, UdpWriter writer, Random r)
        {
            for (int i = 0; i < numAttempts;)
            {
                List<FileSegment> outbound = new List<FileSegment>();
                for (int j = 0; j < burstSize; ++j, ++i)
                {
                    byte[] b = new byte[r.Next(writer.BlockSize - (int)FileSegment.GetSegmentOverhead(0, mtu))];
                    r.NextBytes(b);
                    FileSegment seg = new FileSegment();
                    seg.SegmentId = i;
                    seg.Data = b;
                    outbound.Add(seg);
                }

                await writer.SendMulticast(outbound);
                List<FileSegment> inbound = new List<FileSegment>();
                for (int j = 0; j < 100 && inbound.Count < outbound.Count; ++j)
                {
                    inbound.AddRange(await reader.ReceiveMulticast(Constants.ReadDelay));
                }

                Assert.Equal(outbound.Count, inbound.Count);
                for (int j = 0; j < outbound.Count; ++j)
                {
                    Assert.True(inbound.Contains(outbound[j]));
                }
            }
        }

        private class MulticastSettings : IMulticastSettings
        {
            internal MulticastSettings(IEncoderFactory encoder, int bufferSize, int ttl)
            {
                this.Encoder = encoder;
                this.MulticastBufferSize = bufferSize;
                this.ReadTimeout = TimeSpan.FromMinutes(10);
                this.Ttl = ttl;
                this.RootFolder = FileSystem.Current.LocalStorage;
            }

            public IEncoderFactory Encoder
            {
                get;
                set;
            }

            public int MulticastBufferSize
            {
                get;
                set;
            }

            public TimeSpan ReadTimeout
            {
                get;
                set;
            }

            public IFolder RootFolder
            {
                get;
                set;
            }

            public int Ttl
            {
                get;
                set;
            }
        }

        private class MulticastServerSettings : IMulticastServerSettings
        {
            internal MulticastServerSettings(bool ipv6, int mtu)
            {
                this.DelayCalculation = DelayCalculation.AverageThroughput;
                this.Ipv6 = ipv6;
                this.Mtu = mtu;
                this.MulticastAddress = "239.0.0.1";
                this.MulticastStartPort = 8000;
                this.MulticastBurstLength = 1000;
            }

            public DelayCalculation DelayCalculation
            {
                get;
                set;
            }

            public string InterfaceName
            {
                get;
                set;
            }

            public bool Ipv6
            {
                get;
                set;
            }

            public long MaxBytesPerSecond
            {
                get;
                set;
            }

            public int MaxConnections
            {
                get;
                set;
            }

            public int MaxSessions
            {
                get;
                set;
            }

            public int Mtu
            {
                get;
                set;
            }

            public string MulticastAddress
            {
                get;
                set;
            }

            public int MulticastBurstLength
            {
                get;
                set;
            }

            public int MulticastStartPort
            {
                get;
                set;
            }
        }
    }
}
