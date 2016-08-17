// <copyright file="DownloadTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Core;
    using Core.Cryptography;
    using Core.IO;
    using Core.Server;
    using IO;
    using PCLStorage;
    using Session;
    using Xunit;
    using Xunit.Abstractions;
    using SIO = System.IO;

    public class DownloadTest
    {
        private static readonly TimeSpan ReadTimeout = TimeSpan.FromMinutes(5);

        public DownloadTest(ITestOutputHelper outputHelper)
        {
            LogManager.Adapter = new TestOutputLoggerFactoryAdapter(LogLevel.All, outputHelper);
        }

        [Theory]
        [InlineData(1 << 20, long.MaxValue, 30, new long[] { 1234 }, 1500, 1)]
        [InlineData(1 << 20, 10 << 20, 300, new long[] { 1234, 150000, 150000, 15000000 }, 1500, .8)]
        public async Task ClientDownloadsFolderFromServerNoCrypto(int bufferSize, long maxBytesPerSecond, int readTimeout, long[] fileSizes, int mtu, double packetReception)
        {
            string ins = "in";
            string outs = "out";
            IFolder inFolder = await CreateTestPayload(FileSystem.Current.LocalStorage, ins, fileSizes);
            IFolder outFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync(outs, CreationCollisionOption.ReplaceExisting);
            MulticastSettings serverSettings = new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, inFolder);
            MulticastSettings clientSettings = new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, outFolder);
            MulticastServerSettings serverMulticastSettings = new MulticastServerSettings(DelayCalculation.MaximumThroughput, null, false, maxBytesPerSecond, int.MaxValue, int.MaxValue, mtu, "239.0.0.1", 8000, 100);
            IUdpMulticastFactory multicastFactory = PortableTestUdpMulticast.CreateFactory(mtu, packetReception);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MulticastServer server = new MulticastServer(multicastFactory, new Uri("mc://localhost:801"), serverSettings, serverMulticastSettings))
            {
                cts.CancelAfter(ReadTimeout);
                CancellationToken token = cts.Token;
                try
                {
                    Task listenTask = server.Listen(token);
                    using (MulticastClient client = new MulticastClient(multicastFactory, new Uri("mc://localhost:801"), clientSettings))
                    {
                        await client.StartTransfer(token);
                    }

                    cts.Cancel();
                    try
                    {
                        await listenTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
                finally
                {
                    await server.Close();
                }
            }

            await Task.Delay(10000);
            await CompareTestPayloadFolders(FileSystem.Current.LocalStorage, ins, outs);
        }

        [Theory]
        [InlineData(1 << 20, long.MaxValue, 30, new long[] { 1234 }, 1500, 1)]
        [InlineData(1 << 20, long.MaxValue, 30, new long[] { 1234, 5678 }, 1500, 1)]
        [InlineData(1 << 20, 10 << 20, 300, new long[] { 1234, 150000, 150000, 15000000 }, 1500, .8)]
        public async Task ClientDownloadsMultiFoldersFromServerNoCrypto(int bufferSize, long maxBytesPerSecond, int readTimeout, long[] fileSizes, int mtu, double packetReception)
        {
            IFolder inMulti = await FileSystem.Current.LocalStorage.CreateFolderAsync("inmulti", CreationCollisionOption.ReplaceExisting);
            IFolder outMulti = await FileSystem.Current.LocalStorage.CreateFolderAsync("outmulti", CreationCollisionOption.ReplaceExisting);
            List<string> ins = new List<string>();
            for (int i = 0; i < fileSizes.Length; ++i)
            {
                ins.Add("in" + i);
                IFolder inFolder = await CreateTestPayload(inMulti, ins[i], new long[] { fileSizes[i] });
                IFolder outFolder = await outMulti.CreateFolderAsync(ins[i], CreationCollisionOption.ReplaceExisting);
            }

            MulticastSettings serverSettings = new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, inMulti);
            MulticastServerSettings serverMulticastSettings = new MulticastServerSettings(DelayCalculation.MaximumThroughput, null, false, maxBytesPerSecond, int.MaxValue, int.MaxValue, mtu, "239.0.0.1", 8000, 100);
            IUdpMulticastFactory multicastFactory = PortableTestUdpMulticast.CreateFactory(mtu, packetReception);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MulticastServer server = new MulticastServer(multicastFactory, new Uri("mc://localhost:801"), serverSettings, serverMulticastSettings))
            {
                cts.CancelAfter(ReadTimeout);
                CancellationToken token = cts.Token;
                try
                {
                    Task listenTask = server.Listen(token);
                    List<MulticastClient> clients = new List<MulticastClient>();
                    List<Task> downloadTasks = new List<Task>();
                    for (int i = 0; i < fileSizes.Length; ++i)
                    {
                        clients.Add(new MulticastClient(multicastFactory, new Uri("mc://localhost:801/" + ins[i]), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await outMulti.CreateFolderAsync(ins[i], CreationCollisionOption.ReplaceExisting))));
                    }

                    try
                    {
                        foreach (MulticastClient client in clients)
                        {
                            downloadTasks.Add(client.StartTransfer(token));
                        }

                        await Task.WhenAll(downloadTasks);
                    }
                    finally
                    {
                        foreach (MulticastClient client in clients)
                        {
                            client.Dispose();
                        }
                    }

                    cts.Cancel();
                    try
                    {
                        await listenTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
                finally
                {
                    await server.Close();
                }
            }

            for (int i = 0; i < fileSizes.Length; ++i)
            {
                await Task.Delay(10000);
                await CompareTestPayloadFolders(FileSystem.Current.LocalStorage, "inmulti\\" + ins[i], "outmulti\\" + ins[i]);
            }
        }

        [Theory]
        [InlineData(1 << 20, long.MaxValue, 300, 15000000, 1500, .5)]
        public async Task ClientDownloadsFileFromServerNoCrypto(int bufferSize, long maxBytesPerSecond, int readTimeout, long fileSize, int mtu, double packetReception)
        {
            IFolder inFolder = await CreateTestPayload(FileSystem.Current.LocalStorage, "in2", new long[] { fileSize });
            IFolder outFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("out2", CreationCollisionOption.ReplaceExisting);
            MulticastSettings serverSettings = new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, inFolder);
            MulticastSettings clientSettings = new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, outFolder);
            MulticastServerSettings serverMulticastSettings = new MulticastServerSettings(DelayCalculation.MaximumThroughput, null, false, maxBytesPerSecond, int.MaxValue, int.MaxValue, mtu, "239.0.0.1", 8000, 100);
            IUdpMulticastFactory multicastFactory = PortableTestUdpMulticast.CreateFactory(mtu, packetReception);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MulticastServer server = new MulticastServer(multicastFactory, new Uri("mc://localhost"), serverSettings, serverMulticastSettings))
            {
                cts.CancelAfter(ReadTimeout);
                CancellationToken token = cts.Token;
                try
                {
                    Task listenTask = server.Listen(token);
                    using (MulticastClient client = new MulticastClient(multicastFactory, new Uri("mc://localhost"), clientSettings))
                    {
                        await client.StartTransfer(token);
                    }

                    cts.Cancel();
                    try
                    {
                        await listenTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
                finally
                {
                    await server.Close();
                }
            }

            await Task.Delay(10000);
            await CompareTestPayloadFiles(FileSystem.Current.LocalStorage, "in2\\test0", "out2\\test0");
        }

        [Theory]
        [InlineData("foobar", 1 << 20, long.MaxValue, 300, new long[] { 1234, 150000, 150000, 15000000 }, 1500, .5)]
        public async Task ClientDownloadsFolderFromServerCrypto(string passPhrase, int bufferSize, long maxBytesPerSecond, int readTimeout, long[] fileSizes, int mtu, double packetReception)
        {
            PassphraseEncoderFactory encoder = new PassphraseEncoderFactory(passPhrase, Encoding.Unicode);
            IFolder inFolder = await CreateTestPayload(FileSystem.Current.LocalStorage, "in3", fileSizes);
            IFolder outFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("out3", CreationCollisionOption.ReplaceExisting);
            MulticastSettings serverSettings = new MulticastSettings(encoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, inFolder);
            MulticastSettings clientSettings = new MulticastSettings(encoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, outFolder);
            MulticastServerSettings serverMulticastSettings = new MulticastServerSettings(DelayCalculation.MaximumThroughput, null, false, maxBytesPerSecond, int.MaxValue, int.MaxValue, mtu, "239.0.0.1", 8000, 100);
            IUdpMulticastFactory multicastFactory = PortableTestUdpMulticast.CreateFactory(mtu, packetReception);
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MulticastServer server = new MulticastServer(multicastFactory, new Uri("mcs://localhost"), serverSettings, serverMulticastSettings))
            {
                CancellationToken token = cts.Token;
                try
                {
                    Task listenTask = server.Listen(token);
                    using (MulticastClient client = new MulticastClient(multicastFactory, new Uri("mcs://localhost"), clientSettings))
                    {
                        await client.StartTransfer(token);
                    }

                    cts.Cancel();
                    try
                    {
                        await listenTask;
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }
                finally
                {
                    await server.Close();
                }
            }

            await Task.Delay(10000);
            await CompareTestPayloadFolders(FileSystem.Current.LocalStorage, "in3", "out3");
        }

        private static async Task CompareTestPayloadFolders(IFolder folder, string folder1, string folder2)
        {
            IFolder f1 = await folder.CreateFolderAsync(folder1, CreationCollisionOption.OpenIfExists);
            IFolder f2 = await folder.CreateFolderAsync(folder2, CreationCollisionOption.OpenIfExists);
            foreach (string file in await f1.GetFilesFromPath(true, (f) => true))
            {
                await CompareTestPayloadFiles(folder, PortablePath.Combine(f1.Path, file), PortablePath.Combine(f2.Path, file));
            }
        }

        private static async Task CompareTestPayloadFiles(IFolder folder, string file1, string file2)
        {
            IFile f1 = await folder.CreateFileAsync(file1, CreationCollisionOption.OpenIfExists);
            Assert.NotNull(f1);
            IFile f2 = await folder.CreateFileAsync(file2, CreationCollisionOption.OpenIfExists);
            Assert.NotNull(f2);
            using (SIO.Stream s1 = await f1.OpenAsync(FileAccess.Read))
            using (SIO.Stream s2 = await f2.OpenAsync(FileAccess.Read))
            {
                Assert.Equal(s1.Length, s2.Length);
                for (long j = 0; j < s1.Length;)
                {
                    long toRead = s1.Length - j;
                    if (toRead > (1 << 20))
                    {
                        toRead = 1 << 20;
                    }

                    byte[] b1 = new byte[toRead];
                    await s1.ReadAsync(b1, 0, (int)toRead);
                    byte[] b2 = new byte[toRead];
                    await s2.ReadAsync(b2, 0, (int)toRead);
                    Assert.True(b1.SequenceEqual(b2));
                    j += toRead;
                }
            }
        }

        private static async Task<IFolder> CreateTestPayload(IFolder folder, string folderName, long[] fileSizes)
        {
            IFolder inFolder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.ReplaceExisting);
            for (int i = 0; i < fileSizes.Length; ++i)
            {
                IFile testFile = await inFolder.CreateFileAsync("test" + i, CreationCollisionOption.ReplaceExisting);
                using (SIO.Stream s = await testFile.OpenAsync(FileAccess.ReadAndWrite))
                {
                    Random r = new Random();
                    long szFile = fileSizes[i];
                    for (long j = 0; j < szFile;)
                    {
                        long toWrite = szFile - j;
                        if (toWrite > (1 << 20))
                        {
                            toWrite = 1 << 20;
                        }

                        byte[] b = new byte[toWrite];
                        j += toWrite;
                        r.NextBytes(b);
                        await s.WriteAsync(b, 0, b.Length);
                    }
                }
            }

            return inFolder;
        }

        private class MulticastSettings : IMulticastSettings
        {
            internal MulticastSettings(IEncoderFactory encoder, int bufferSize, TimeSpan readTimeout, int ttl, IFolder folder)
            {
                this.Encoder = encoder;
                this.MulticastBufferSize = bufferSize;
                this.ReadTimeout = readTimeout;
                this.Ttl = ttl;
                this.RootFolder = folder;
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
            internal MulticastServerSettings(DelayCalculation delayCalculation, string interfaceName, bool ipv6, long maxBytesPerSecond, int maxSessions, int maxConnections, int mtu, string multicastAddress, int multicastBurstLength, int multicastStartPort)
            {
                this.DelayCalculation = delayCalculation;
                this.InterfaceName = interfaceName;
                this.Ipv6 = ipv6;
                this.MaxBytesPerSecond = maxBytesPerSecond;
                this.MaxSessions = maxSessions;
                this.MaxConnections = maxConnections;
                this.Mtu = mtu;
                this.MulticastAddress = multicastAddress;
                this.MulticastStartPort = multicastStartPort;
                this.MulticastBurstLength = multicastBurstLength;
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
