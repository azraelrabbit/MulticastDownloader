// <copyright file="MulticastClient.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using IO;
    using Properties;
    using Session;
    using Sockets;

    /// <summary>
    /// Represent a multicast client.
    /// </summary>
    public class MulticastClient : IDisposable
    {
        internal const int PacketUpdateInterval = 1000;
        internal const int EncoderSize = 256;
        internal static readonly byte[] ResponseId = Encoding.UTF8.GetBytes("client");

        private ILog log = LogManager.GetLogger<MulticastClient>();
        private IEncoderFactory fileEncoder;
        private CancellationToken token;
        private FileSet fileSet;
        private FileChunk[] chunks;
        private BitVector written;
        private ClientConnection cliConn;
        private ChunkWriter writer;
        private UdpReader udpReader;
        private int state;
        private bool tcpDownload = false;
        private bool disposedValue = false;
        private bool complete = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastClient"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastClient(Uri path, IMulticastSettings settings)
            : this(path, settings, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastClient"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="state">The application-defined state.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastClient(Uri path, IMulticastSettings settings, int state)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            this.Parameters = new UriParameters(path);
            if (this.Parameters.UseTls && this.Settings.Encoder == null)
            {
                throw new ArgumentException(Resources.MustSpecifyEncoder, "settings");
            }

            this.Settings = settings;
            this.token = CancellationToken.None;
            this.state = state;
        }

        /// <summary>
        /// Gets the multicast URI parameters.
        /// </summary>
        /// <value>
        /// The URI parameters.
        /// </value>
        public UriParameters Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the multicast settings.
        /// </summary>
        /// <value>
        /// The multicast settings.
        /// </value>
        public IMulticastSettings Settings
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MulticastClient"/> is complete.
        /// </summary>
        /// <value>
        ///   <c>true</c> if complete; otherwise, <c>false</c>.
        /// </value>
        public bool Complete
        {
            get
            {
                return this.complete;
            }
        }

        /// <summary>
        /// Gets the total bytes in the payload.
        /// </summary>
        /// <value>
        /// The total bytes in the payload.
        /// </value>
        public long TotalBytes
        {
            get
            {
                if (this.writer != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in this.writer.Chunks)
                    {
                        bytes += chunk.Block.Length;
                    }

                    return bytes;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the bytes remaining in the payload.
        /// </summary>
        /// <value>
        /// The bytes remaining.
        /// </value>
        public long BytesRemaining
        {
            get
            {
                if (this.writer != null)
                {
                    long bytes = 0;
                    foreach (FileChunk chunk in this.writer.Chunks)
                    {
                        if (!this.writer.Written[chunk.Block.SegmentId])
                        {
                            bytes += chunk.Block.Length;
                        }
                    }

                    return bytes;
                }

                return 0;
            }
        }

        /// <summary>
        /// Starts the multicast transfer.
        /// </summary>
        /// <param name="token">The optional cancellation token.</param>
        /// <returns>A task object.</returns>
        /// <exception cref="SessionAbortedException">Occurs if the transfer is aborted.</exception>
        public Task StartTransfer(CancellationToken token)
        {
            this.token = token;
            return Task.Run(async () =>
            {
                try
                {
                    this.log.Debug("Starting transfer");

                    // Connect to the server, authenticate and receive the multicast payload.
                    await this.ConnectToServer();
                    await this.RequestFilesAndBeginReading();

                    // Now begin processing received packets and transmitting them back to the service.
                    await this.MulticastDownload();

                    // Finally, if we're receiving the rest of our payload through TCP, download it here.
                    if (this.tcpDownload)
                    {
                        await this.TcpDownload();
                    }

                    await this.writer.Flush();
                    this.log.Info("Transfer complete");
                    this.complete = true;
                    await this.cliConn.Close();
                    await this.udpReader.Close();
                }
                catch (Exception ex)
                {
                    this.log.Fatal(ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                if (disposing)
                {
                    if (this.cliConn != null)
                    {
                        this.cliConn.Dispose();
                    }

                    if (this.udpReader != null)
                    {
                        this.udpReader.Dispose();
                    }

                    if (this.fileSet != null)
                    {
                        this.fileSet.Dispose();
                    }
                }

                this.written = null;
                this.chunks = null;
                this.writer = null;
            }
        }

        private async Task ConnectToServer()
        {
            try
            {
                this.log.Info("Connecting to server: " + this.Parameters);
                this.cliConn = new ClientConnection(this.Parameters, this.Settings);
                await this.cliConn.Connect();

                Challenge challenge = await this.cliConn.Receive<Challenge>(this.token);
                byte[] psk = null;
                if (this.Settings.Encoder != null)
                {
                    IDecoder decoder = this.Settings.Encoder.CreateDecoder();
                    psk = decoder.Decode(challenge.ChallengeKey);
                }
                else
                {
                    psk = challenge.ChallengeKey;
                }

                this.log.Debug("Challenge: " + Convert.ToBase64String(challenge.ChallengeKey) + " PSK: " + Convert.ToBase64String(psk));
                if (this.Parameters.UseTls)
                {
                    // We use TLS to secure the connection before sending back the decoded response.
                    this.cliConn.ConnectTls(psk);
                }

                // We'll use the file encoder on the PSK to decode file data.
                this.fileEncoder = new HashEncoderFactory(psk, EncoderSize);
                IEncoder encoder = this.fileEncoder.CreateEncoder();

                ChallengeResponse response = new ChallengeResponse();
                response.ChallengeKey = encoder.Encode(ResponseId);
                this.log.Debug("Response: " + Convert.ToBase64String(psk));
                await this.cliConn.Send(response, this.token);

                Response authResponse = await this.CheckResponse<Response>();
                this.log.Debug("Auth response: " + authResponse);
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.ConnectionFailed, ex);
            }
        }

        private async Task RequestFilesAndBeginReading()
        {
            try
            {
                SessionJoinRequest request = new SessionJoinRequest();
                request.Path = this.Parameters.Path;
                request.State = this.state;
                this.log.InfoFormat("Requesting payload '{0}' with state: {1}", request.Path, request.State);
                await this.cliConn.Send(request, this.token);

                SessionJoinResponse response = await this.CheckResponse<SessionJoinResponse>();
                this.fileSet = new FileSet(this.Settings.RootFolder, response.Files);
                await this.fileSet.InitWrite();
                this.chunks = this.fileSet.EnumerateChunks().ToArray();
                this.written = new BitVector(this.chunks.LongCount());
                this.writer = new ChunkWriter(this.chunks, this.written);
                this.log.Debug("Bytes remaining: " + this.BytesRemaining);

                this.log.DebugFormat("Listening on {0}:{1}", response.MulticastAddress, response.MulticastPort);
                this.udpReader = await this.cliConn.JoinMulticastServer(response, this.fileEncoder);
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.RequestFailed, ex);
            }
        }

        private async Task MulticastDownload()
        {
            try
            {
                this.log.Info("Waiting for multicast payload");
                Task statusTask = this.UpdateStatus();
                Task writeTask = null;
                while (!this.tcpDownload && this.written.Contains(false))
                {
                    this.token.ThrowIfCancellationRequested();
                    IEnumerable<FileSegment> received = await this.udpReader.ReceiveMulticast<FileSegment>(this.token);
                    if (writeTask != null)
                    {
                        await writeTask;
                    }

                    writeTask = this.writer.WriteSegments(received);
                }

                if (writeTask != null)
                {
                    await writeTask;
                }

                if (statusTask != null)
                {
                    await statusTask;
                }
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.MulticastDownloadFailed, ex);
            }
        }

        private async Task TcpDownload()
        {
            try
            {
                this.log.Info("Waiting for TCP payload");
                Task lastWrite = null;
                while (this.written.Contains(false))
                {
                    FileSegment segment = await this.cliConn.Receive<FileSegment>(this.token);
                    if (lastWrite != null)
                    {
                        await lastWrite;
                    }

                    lastWrite = this.writer.WriteSegments(new FileSegment[] { segment });
                }

                if (lastWrite != null)
                {
                    await lastWrite;
                }
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.TcpDownloadFailed, ex);
            }
        }

        // Returns true if the wave completed
        private Task UpdateStatus()
        {
            return Task.Run(async () =>
            {
                try
                {
                    bool leavingSession = false;
                    while (leavingSession = this.written.Contains(false))
                    {
                        await Task.Delay(PacketUpdateInterval, this.token);
                        PacketStatusUpdate statusUpdate = new PacketStatusUpdate();
                        long bytesLeft = 0;
                        for (long i = 0; i < this.written.LongCount; ++i)
                        {
                            if (this.written[i])
                            {
                                bytesLeft += this.chunks[i].Block.Length;
                            }
                        }

                        statusUpdate.BytesLeft = bytesLeft;
                        statusUpdate.LeavingSession = leavingSession;
                        this.log.Debug("Bytes left: " + bytesLeft + " Leaving session: " + leavingSession);
                        await this.cliConn.Send(statusUpdate, this.token);
                        Response resp = await this.CheckResponse<Response>();
                        if (resp.ResponseType == Session.ResponseId.WaveComplete)
                        {
                            this.log.Debug("Wave complete");
                            WaveStatusUpdate waveUpdate = new WaveStatusUpdate();
                            waveUpdate.BytesLeft = bytesLeft;
                            waveUpdate.LeavingSession = leavingSession;
                            waveUpdate.FileBitVector = this.written.RawBits;
                            await this.cliConn.Send(waveUpdate, this.token);
                            WaveCompleteResponse waveResp = await this.CheckResponse<WaveCompleteResponse>();
                            this.tcpDownload = waveResp.DirectDownload;
                            if (this.tcpDownload)
                            {
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new SessionAbortedException(Resources.StatusUpdateFailed, ex);
                }
            });
        }

        private async Task<T> CheckResponse<T>()
            where T : Response
        {
            T authResponse = await this.cliConn.Receive<T>(this.token);
            authResponse.ThrowIfFailed();
            return authResponse;
        }
    }
}
