// <copyright file="MulticastClient.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Cryptography;
    using IO;
    using PCLStorage;
    using Properties;
    using Session;

    /// <summary>
    /// Represent a multicast client.
    /// </summary>
    /// <see cref="ITransferReporting" />
    /// <see cref="ISequenceReporting" />
    /// <see cref="IReceptionReporting" />
    public class MulticastClient : IDisposable, ITransferReporting, ISequenceReporting, IReceptionReporting
    {
        private ILog log = LogManager.GetLogger<MulticastClient>();
        private IEncoderFactory fileEncoder;
        private CancellationToken token;
        private FileSet fileSet;
        private BitVector written;
        private ClientConnection cliConn;
        private ChunkWriter writer;
        private IUdpMulticastFactory udpMulticast;
        private UdpReader<FileSegment> udpReader;
        private BoxedLong seqNum;
        private BoxedLong waveNum;
        private BoxedDouble receptionRate = new BoxedDouble(1.0);
        private ThroughputCalculator throughputCalculator = new ThroughputCalculator(Constants.MaxIntervals);
        private BoxedLong bytesPerSecond;
        private int state;
        private bool disposedValue = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastClient"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastClient(Uri path, IMulticastSettings settings)
            : this(PortableUdpMulticast.Factory, path, settings)
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
            : this(PortableUdpMulticast.Factory, path, settings, state)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastClient"/> class.
        /// </summary>
        /// <param name="udpMulticast">The <see cref="IUdpMulticastFactory"/> implementation.</param>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastClient(IUdpMulticastFactory udpMulticast, Uri path, IMulticastSettings settings)
            : this(udpMulticast, path, settings, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MulticastClient"/> class.
        /// </summary>
        /// <param name="udpMulticast">The <see cref="IUdpMulticast"/> implementation.</param>
        /// <param name="path">The path.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="state">The application-defined state.</param>
        /// <remarks>For examples of possible multicast URIs, see the <see cref="UriParameters"/> class.</remarks>
        public MulticastClient(IUdpMulticastFactory udpMulticast, Uri path, IMulticastSettings settings, int state)
        {
            if (udpMulticast == null)
            {
                throw new ArgumentNullException("udpMulticast");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            settings.Validate();
            this.Parameters = new UriParameters(path);
            this.Settings = settings;
            if (this.Parameters.UseTls && this.Settings.Encoder == null)
            {
                throw new ArgumentException(Resources.MustSpecifyEncoder, "settings");
            }

            this.token = CancellationToken.None;
            this.state = state;
            this.udpMulticast = udpMulticast;
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
        /// Gets the total bytes in the payload.
        /// </summary>
        /// <value>
        /// The total bytes in the payload.
        /// </value>
        public long TotalBytes
        {
            get
            {
                ChunkWriter writer = this.writer;
                if (writer != null)
                {
                    return writer.TotalBytes;
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
                ChunkWriter writer = this.writer;
                if (writer != null)
                {
                    return writer.BytesRemaining;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the bytes per second.
        /// </summary>
        /// <value>
        /// The bytes per second.
        /// </value>
        public long BytesPerSecond
        {
            get
            {
                BoxedLong bytesPerSecond = this.bytesPerSecond;
                if (bytesPerSecond != null)
                {
                    return bytesPerSecond.Value;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the written bits.
        /// </summary>
        /// <value>
        /// The written bits.
        /// </value>
        public BitVector Written
        {
            get
            {
                return this.written;
            }
        }

        /// <summary>
        /// Gets the current sequence number in the download.
        /// </summary>
        /// <value>
        /// The sequence number.
        /// </value>
        public long SequenceNumber
        {
            get
            {
                BoxedLong seq = this.seqNum;
                if (seq != null)
                {
                    return seq.Value;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the current wave number in the download.
        /// </summary>
        /// <value>
        /// The wave number.
        /// </value>
        public long WaveNumber
        {
            get
            {
                BoxedLong wave = this.waveNum;
                if (wave != null)
                {
                    return wave.Value;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the packet reception rate reported from the last wave update.
        /// </summary>
        /// <value>
        /// The reception rate, as a coefficient in the range [0,1].
        /// </value>
        public double ReceptionRate
        {
            get
            {
                BoxedDouble receptionRate = this.receptionRate;
                if (receptionRate != null)
                {
                    return receptionRate.Value;
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
                    this.log.Debug("client: Starting transfer");

                    // Connect to the server, authenticate and receive the multicast payload.
                    await this.ConnectToServer();
                    SessionJoinResponse response = await this.RequestFilesAndBeginReading();

                    // Now begin processing received packets and transmitting them back to the service.
                    await this.MulticastDownload(response);

                    await this.writer.Flush();
                    this.log.Info("client: Transfer complete");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    this.log.Error("client: Session aborted: " + ex.GetType() + " '" + ex.Message + "'");
                    await this.TryCleanFiles();
                    if (!(ex is SessionAbortedException))
                    {
                        throw new SessionAbortedException(Resources.SessionAborted, ex);
                    }

                    throw;
                }
            });
        }

        /// <summary>
        /// Closes this client and any connections associated with it.
        /// </summary>
        /// <returns>A task object</returns>
        public async Task Close()
        {
            if (this.udpReader != null)
            {
                await this.udpReader.Close();
            }

            if (this.cliConn != null)
            {
                await this.cliConn.Close();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        internal async Task ConnectToServer()
        {
            try
            {
                this.log.Info("client: Connecting to server: " + this.Parameters);
                this.cliConn = new ClientConnection(this.Parameters, this.Settings);
                await this.cliConn.Connect();
                Response connectResponse = this.cliConn.Receive<Response>(this.token);
                connectResponse.ThrowIfFailed();

                Challenge challenge = this.cliConn.Receive<Challenge>(this.token);
                byte[] psk = null;
                if (this.Settings.Encoder != null)
                {
                    IDecoder decoder = this.Settings.Encoder.CreateDecoder();
                    psk = decoder.Decode(challenge.ChallengeKey);
                }

                this.log.Debug("client: Challenge: " + ((challenge.ChallengeKey != null) ? Convert.ToBase64String(challenge.ChallengeKey) : "<null>"));
                if (this.Parameters.UseTls)
                {
                    // We use TLS to secure the connection before sending back the decoded response.
                    this.cliConn.ConnectTls(psk);
                }

                // We'll use the file encoder on the PSK to decode file data.
                ChallengeResponse challengeResponse = new ChallengeResponse();
                if (psk != null)
                {
                    this.fileEncoder = new HashEncoderFactory(psk, Constants.EncoderSize);
                    IEncoder encoder = this.fileEncoder.CreateEncoder();
                    challengeResponse.ChallengeKey = encoder.Encode(Constants.ResponseId);
                }

                this.cliConn.Send(challengeResponse, this.token);

                Response authResponse = this.CheckResponse<Response>();
                this.log.Debug("client: Auth response: " + authResponse);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.ConnectionFailed, ex);
            }
        }

        internal async Task<SessionJoinResponse> RequestFilesAndBeginReading()
        {
            try
            {
                SessionJoinRequest request = new SessionJoinRequest();
                request.Path = this.Parameters.Path;
                request.State = this.state;
                this.log.InfoFormat("client: Requesting payload '{0}' with state: {1}", request.Path, request.State);
                this.cliConn.Send(request, this.token);

                SessionJoinResponse response = this.CheckResponse<SessionJoinResponse>();
                this.fileSet = new FileSet(this.Settings.RootFolder, response.Files);
                await this.fileSet.InitWrite();
                if (this.written == null)
                {
                    long countChunks = this.fileSet.EnumerateChunks().LongCount();
                    this.written = new BitVector(countChunks);
                }

                this.writer = new ChunkWriter(this.fileSet.EnumerateChunks(), this.written);
                this.log.Debug("client: Bytes remaining: " + this.BytesRemaining);

                this.log.DebugFormat("client: Listening on {0}:{1}", response.MulticastAddress, response.MulticastPort);
                this.waveNum = new BoxedLong(response.WaveNumber);
                this.log.Debug("client: Starting wave: " + response.WaveNumber);
                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.RequestFailed, ex);
            }
        }

        internal async Task MulticastDownload(SessionJoinResponse response)
        {
            try
            {
                //// It could be a while until we join an actual wave, so disable the read timeout on our status socket
                //// until we get a packet status update response.
                this.cliConn.TcpSession.ReadStream.ReadTimeout = int.MaxValue;
                this.udpReader = await this.cliConn.JoinMulticastServer(this.udpMulticast.CreateMulticast(), response, this.fileEncoder);
                this.throughputCalculator.Start(this.BytesRemaining, DateTime.Now);
                this.log.Info("client: Waiting for multicast payload");

                BoxedLong bytesReceived = new BoxedLong(0);
                Func<Task<long>> createStatusTask = () =>
                {
                    return Task.Run(() =>
                    {
                        long ret = this.BytesRemaining;
                        if (this.UpdatePacketStatus(bytesReceived.Value, ret))
                        {
                            this.UpdateWaveStatus(bytesReceived.Value, ret);
                        }

                        if (this.cliConn.TcpSession.ReadStream.ReadTimeout == int.MaxValue)
                        {
                            this.cliConn.TcpSession.ReadStream.ReadTimeout = (int)this.Settings.ReadTimeout.TotalMilliseconds;
                        }

                        return ret;
                    });
                };

                Func<Task<ICollection<FileSegment>>> createReadTask = async () =>
                {
                    ICollection<FileSegment> ret = await this.udpReader.ReceiveMulticast(Constants.ReadDelay);
                    long received = bytesReceived.Value;
                    foreach (FileSegment segment in ret)
                    {
                        if (this.log.IsTraceEnabled)
                        {
                            this.log.TraceFormat("R ID: {0}, len: {1}", segment.SegmentId, segment.Data.Length);
                        }

                        received += segment.Data.Length;
                    }

                    bytesReceived = new BoxedLong(received);
                    return ret;
                };

                Func<ICollection<FileSegment>, Task> createWriteTask = async (l) =>
                {
                    await this.writer.WriteSegments(l);
                    FileSegment last = l.LastOrDefault();
                    if (last != null)
                    {
                        this.seqNum = new BoxedLong(last.SegmentId);
                    }
                };

                Task<long> statusTask = createStatusTask();
                Task<ICollection<FileSegment>> readTask = createReadTask();
                Task writeTask = Task.Run(() => { });
                long bytesLeft = this.BytesRemaining;
                while (bytesLeft > 0)
                {
                    Task waited = await Task.WhenAny(statusTask, readTask);
                    if (waited == statusTask)
                    {
                        bytesLeft = await statusTask;
                        if (bytesLeft > 0)
                        {
                            statusTask = createStatusTask();
                        }
                    }
                    else if (waited == readTask)
                    {
                        ICollection<FileSegment> received = await readTask;
                        readTask = createReadTask();
                        await writeTask;
                        writeTask = createWriteTask(received);
                    }
                }

                await Task.WhenAll(readTask, writeTask, statusTask);
                if (this.written.Contains(false) || this.BytesRemaining > 0)
                {
                    throw new InvalidOperationException();
                }

                this.log.Debug("client: Complete. Leaving session.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.MulticastDownloadFailed, ex);
            }
        }

        // Returns true if the wave completed and we need to update our wave status
        internal bool UpdatePacketStatus(long bytesRecieved, long bytesLeft)
        {
            try
            {
                PacketStatusUpdate statusUpdate = new PacketStatusUpdate();
                long average = this.throughputCalculator.UpdateThroughput(bytesLeft, DateTime.Now);
                this.bytesPerSecond = new BoxedLong(average);
                statusUpdate.BytesRecieved = bytesRecieved;
                statusUpdate.LeavingSession = bytesLeft == 0;
                this.log.Debug("client: Sequence: " + this.SequenceNumber + "  Bytes left: " + bytesLeft + " Bytes per second: " + this.BytesPerSecond + "  Leaving session: " + (bytesLeft == 0));
                this.cliConn.Send(statusUpdate, this.token);
                if (!statusUpdate.LeavingSession)
                {
                    PacketStatusUpdateResponse resp = this.CheckResponse<PacketStatusUpdateResponse>();
                    this.receptionRate = new BoxedDouble(resp.ReceptionRate);
                    return resp.ResponseType == ResponseId.WaveComplete && !statusUpdate.LeavingSession;
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.StatusUpdateFailed, ex);
            }
        }

        internal void UpdateWaveStatus(long bytesRecieved, long bytesLeft)
        {
            try
            {
                this.log.Debug("client: Wave complete");
                WaveStatusUpdate waveUpdate = new WaveStatusUpdate();
                waveUpdate.BytesRecieved = bytesRecieved;
                waveUpdate.LeavingSession = bytesLeft == 0;
                this.log.Debug("client: Wave Sequence: " + this.SequenceNumber + "  Bytes left: " + bytesLeft + "  Leaving session: " + (bytesLeft == 0));
                waveUpdate.FileBitVector = this.written.RawBits;
                this.cliConn.Send(waveUpdate, this.token);
                if (!waveUpdate.LeavingSession)
                {
                    WaveCompleteResponse waveResp = this.CheckResponse<WaveCompleteResponse>();
                    this.waveNum = new BoxedLong(waveResp.WaveNumber);
                    this.log.Debug("client: New wave: " + waveResp.WaveNumber);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SessionAbortedException(Resources.StatusUpdateFailed, ex);
            }
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
                    this.DisposeInternal();
                }

                this.written = null;
                this.writer = null;
            }
        }

        private void DisposeInternal()
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

        private async Task<bool> TryCleanFiles()
        {
            try
            {
                if (this.cliConn != null)
                {
                    await this.cliConn.Close();
                }

                if (this.udpReader != null)
                {
                    await this.udpReader.Close();
                }

                if (this.fileSet != null)
                {
                    await this.fileSet.Clean();
                }

                return true;
            }
            catch (Exception ex)
            {
                this.log.Warn("client: Failed to clean temporary files from disk.");
                this.log.Debug(ex);
            }

            return false;
        }

        private T CheckResponse<T>()
            where T : Response
        {
            T authResponse = this.cliConn.Receive<T>(this.token);
            authResponse.ThrowIfFailed();
            return authResponse;
        }
    }
}
