// <copyright file="ClientServerTest.cs" company="MS">
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

    public class ClientServerTest
    {
        public ClientServerTest(ITestOutputHelper outputHelper)
        {
            LogManager.Adapter = new TestOutputLoggerFactoryAdapter(LogLevel.All, outputHelper);
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1)]
        public async Task ClientAuthenticatesWithServerNoCrypto(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            int szFile = await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    Assert.Equal(1, server.Connections.Count);
                    Assert.Equal(1, server.Sessions.Count);
                    Assert.Null(server.ChallengeKey);
                    Assert.Equal(0, server.BytesPerSecond);
                    Assert.Equal(0, server.BytesRemaining);
                    Assert.Equal(null, server.EncoderFactory);
                    Assert.Equal(1, server.ReceptionRate);
                    Assert.Equal(1, client.ReceptionRate);
                    Assert.Equal(szFile, client.TotalBytes);
                    Assert.Equal(client.WaveNumber, server.Sessions.First().WaveNumber);
                    Assert.Equal(0, server.Sessions.First().SessionId);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, "1234")]
        public async Task ClientAuthenticatesWithServerPassphraseCrypto(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, string passPhrase)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IEncoderFactory passphraseEncoder = new PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            IFolder inFolder = await folder.CreateFolderAsync("in", CreationCollisionOption.ReplaceExisting);
            IFile testFile = await inFolder.CreateFileAsync("test1", CreationCollisionOption.ReplaceExisting);
            int szFile = await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    Assert.Equal(1, server.Connections.Count);
                    Assert.Equal(1, server.Sessions.Count);
                    Assert.NotNull(server.ChallengeKey);
                    Assert.Equal(0, server.BytesPerSecond);
                    Assert.Equal(0, server.BytesRemaining);
                    Assert.NotNull(server.EncoderFactory);
                    Assert.Equal(1, server.ReceptionRate);
                    Assert.Equal(1, client.ReceptionRate);
                    Assert.Equal(szFile, client.TotalBytes);
                    Assert.Equal(client.WaveNumber, server.Sessions.First().WaveNumber);
                    Assert.Equal(0, server.Sessions.First().SessionId);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, "1234")]
        public async Task ClientAuthenticatesWithServerPassphraseCryptoTls(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, string passPhrase)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IEncoderFactory passphraseEncoder = new PassphraseEncoderFactory(passPhrase, Encoding.UTF8);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            IFolder inFolder = await folder.CreateFolderAsync("in", CreationCollisionOption.ReplaceExisting);
            IFile testFile = await inFolder.CreateFileAsync("test1", CreationCollisionOption.ReplaceExisting);
            int szFile = await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mcs://localhost/"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    Assert.Equal(1, server.Connections.Count);
                    Assert.Equal(1, server.Sessions.Count);
                    Assert.NotNull(server.ChallengeKey);
                    Assert.Equal(0, server.BytesPerSecond);
                    Assert.Equal(0, server.BytesRemaining);
                    Assert.NotNull(server.EncoderFactory);
                    Assert.Equal(1, server.ReceptionRate);
                    Assert.Equal(1, client.ReceptionRate);
                    Assert.Equal(szFile, client.TotalBytes);
                    Assert.Equal(client.WaveNumber, server.Sessions.First().WaveNumber);
                    Assert.Equal(0, server.Sessions.First().SessionId);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, 2048)]
        public async Task ClientAuthenticatesWithServerPubPrivateCryptoTls(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, int strength)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            await SecretWriter.WriteAsymmetricKeyPair(folder, "client_priv.rsa", "client_pub.rsa", strength);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            int szFile = await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mcs://localhost/"), new MulticastSettings(await AsymmetricEncoderFactory.Load(folder, "client_priv.rsa", AsymmetricSecretFlags.ReadPrivateKey, null), bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(await AsymmetricEncoderFactory.Load(folder, "client_pub.rsa", AsymmetricSecretFlags.None, null), bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    Assert.Equal(1, server.Connections.Count);
                    Assert.Equal(1, server.Sessions.Count);
                    Assert.NotNull(server.ChallengeKey);
                    Assert.Equal(0, server.BytesPerSecond);
                    Assert.Equal(0, server.BytesRemaining);
                    Assert.NotNull(server.EncoderFactory);
                    Assert.Equal(1, server.ReceptionRate);
                    Assert.Equal(1, client.ReceptionRate);
                    Assert.Equal(szFile, client.TotalBytes);
                    Assert.Equal(client.WaveNumber, server.Sessions.First().WaveNumber);
                    Assert.Equal(0, server.Sessions.First().SessionId);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, 2048)]
        public async Task ClientDoesNotAuthenticateAgainstMismatchedPubPrivCryptoTls(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, int strength)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            await SecretWriter.WriteAsymmetricKeyPair(folder, "client_priv.rsa", "client_pub.rsa", strength);
            await SecretWriter.WriteAsymmetricKeyPair(folder, "client2_priv.rsa", "client_pub.rsa", strength);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mcs://localhost/"), new MulticastSettings(await AsymmetricEncoderFactory.Load(folder, "client_priv.rsa", AsymmetricSecretFlags.ReadPrivateKey, null), bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(await AsymmetricEncoderFactory.Load(folder, "client_pub.rsa", AsymmetricSecretFlags.None, null), bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(0, server.Connections.Count);
                    Assert.Equal(0, server.Sessions.Count);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1)]
        public async Task ClientDoesNotAuthenticateAgainstMismatchedPassphraseCrypto(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IEncoderFactory passphraseEncoder1 = new PassphraseEncoderFactory("foo", Encoding.UTF8);
            IEncoderFactory passphraseEncoder2 = new PassphraseEncoderFactory("barbaz", Encoding.UTF8);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mcs://localhost/"), new MulticastSettings(passphraseEncoder1, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(passphraseEncoder2, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(0, server.Connections.Count);
                    Assert.Equal(0, server.Sessions.Count);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, "passphrase")]
        public async Task ClientDoesNotAuthenticateAgainstTlsServerWithPassphraseCrypto(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, string passphrase)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IEncoderFactory passphraseEncoder = new PassphraseEncoderFactory(passphrase, Encoding.UTF8);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            IFolder inFolder = await folder.CreateFolderAsync("in", CreationCollisionOption.ReplaceExisting);
            IFile testFile = await inFolder.CreateFileAsync("test1", CreationCollisionOption.ReplaceExisting);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(passphraseEncoder, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(0, server.Connections.Count);
                    Assert.Equal(0, server.Sessions.Count);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, 2048)]
        public async Task ClientDoesNotAuthenticateAgainstTlsServerWithNoCryptoPubPrivServer(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, int strength)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            await SecretWriter.WriteAsymmetricKeyPair(folder, "client_priv.rsa", "client_pub.rsa", strength);
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            IFolder inFolder = await folder.CreateFolderAsync("in", CreationCollisionOption.ReplaceExisting);
            IFile testFile = await inFolder.CreateFileAsync("test1", CreationCollisionOption.ReplaceExisting);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mcs://localhost/in"), new MulticastSettings(await AsymmetricEncoderFactory.Load(folder, "client_priv.rsa", AsymmetricSecretFlags.ReadPrivateKey, null), bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(0, server.Connections.Count);
                    Assert.Equal(0, server.Sessions.Count);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1, "mc://localhost/foomcbar")]
        public async Task ServerDoesNotAuthorizeAnInvalidPath(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections, string uri)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri(uri), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out", CreationCollisionOption.ReplaceExisting))))
                {
                    await client.ConnectToServer();
                    try
                    {
                        await client.RequestFilesAndBeginReading();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(0, server.Connections.Count);
                    Assert.Equal(0, server.Sessions.Count);
                    await client.Close();
                }

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1, 1)]
        [InlineData(1 << 20, 600, null, false, 1, 10)]
        public async Task ServerDoesNotAuthorizeAClientWithTooManyConnections(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions, int maxConnections)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, maxConnections, 576, "239.0.0.1", 1000, 0xFF00);
            await CreateTestPayload(folder);

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);
                List<MulticastClient<PortableUdpMulticast>> clients = new List<MulticastClient<PortableUdpMulticast>>();
                for (int i = 0; i < maxConnections; ++i)
                {
                    MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out" + i.ToString(), CreationCollisionOption.ReplaceExisting)));
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    clients.Add(client);
                }

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out" + maxConnections.ToString(), CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(maxConnections, server.Connections.Count);
                    Assert.Equal(1, server.Sessions.Count);
                    await client.Close();
                }

                foreach (MulticastClient<PortableUdpMulticast> client in clients)
                {
                    await client.Close();
                    client.Dispose();
                }

                clients.Clear();

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        [Theory]
        [InlineData(1 << 20, 600, null, false, 1)]
        [InlineData(1 << 20, 600, null, false, 3)]
        public async Task ServerDoesNotAuthorizeAClientWithTooManySessions(int bufferSize, int readTimeout, string interfaceName, bool ipv6, int maxSessions)
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            MulticastServerSettings serverSettings = new MulticastServerSettings(DelayCalculation.AverageThroughput, interfaceName, ipv6, 10 << 20, maxSessions, int.MaxValue, 576, "239.0.0.1", 1000, 0xFF00);
            for (int i = 0; i < maxSessions + 1; ++i)
            {
                await CreateTestPayload(folder, "in" + i.ToString());
            }

            using (MulticastServer<PortableUdpMulticast> server = new MulticastServer<PortableUdpMulticast>(new Uri("mc://localhost/"), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, folder), serverSettings))
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                Task serverTask = server.AcceptAndJoinClients(cts.Token);
                List<MulticastClient<PortableUdpMulticast>> clients = new List<MulticastClient<PortableUdpMulticast>>();
                for (int i = 0; i < maxSessions; ++i)
                {
                    MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in" + i.ToString()), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out" + i.ToString(), CreationCollisionOption.ReplaceExisting)));
                    await client.ConnectToServer();
                    await client.RequestFilesAndBeginReading();
                    clients.Add(client);
                }

                using (MulticastClient<PortableUdpMulticast> client = new MulticastClient<PortableUdpMulticast>(new Uri("mc://localhost/in" + maxSessions.ToString()), new MulticastSettings(null, bufferSize, TimeSpan.FromSeconds(readTimeout), 1, await folder.CreateFolderAsync("out" + maxSessions.ToString(), CreationCollisionOption.ReplaceExisting))))
                {
                    try
                    {
                        await client.ConnectToServer();
                        await client.RequestFilesAndBeginReading();
                        Assert.False(true);
                    }
                    catch (Exception)
                    {
                    }

                    Assert.Equal(maxSessions, server.Sessions.Count);
                    await client.Close();
                }

                foreach (MulticastClient<PortableUdpMulticast> client in clients)
                {
                    await client.Close();
                    client.Dispose();
                }

                clients.Clear();

                cts.Cancel();
                try
                {
                    await serverTask;
                }
                catch (OperationCanceledException)
                {
                }

                await server.Close();
            }
        }

        private static async Task<int> CreateTestPayload(IFolder folder, string folderName)
        {
            IFolder inFolder = await folder.CreateFolderAsync(folderName, CreationCollisionOption.ReplaceExisting);
            IFile testFile = await inFolder.CreateFileAsync("test1", CreationCollisionOption.ReplaceExisting);
            int szFile;
            using (SIO.Stream s = await testFile.OpenAsync(FileAccess.ReadAndWrite))
            {
                Random r = new Random();
                szFile = r.Next(1 << 20);
                byte[] b = new byte[szFile];
                r.NextBytes(b);
                await s.WriteAsync(b, 0, b.Length);
            }

            return szFile;
        }

        private static async Task<int> CreateTestPayload(IFolder folder)
        {
            return await CreateTestPayload(folder, "in");
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
