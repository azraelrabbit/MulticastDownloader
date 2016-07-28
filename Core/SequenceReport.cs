// <copyright file="SequenceReport.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Session;

    /// <summary>
    /// Represent a viewable sequence report.
    /// </summary>
    public class SequenceReport
    {
        private const int SeqColor = 0xFFFF00;
        private static readonly int[] Palette = new int[] { 0x4B3B47, 0x6A6262, 0x9C9990, 0xCFD2B2, 0xE0D8DE, 0x114B5F, 0x456990, 0xF45B69, 0xCFD2B2, 0x6B2737 };

        private ISequenceReporting sequenceReporting;
        private long seqNum;
        private long waveNum;
        private byte[] seqMap24BppRgb;
        private BitVector writtenVector;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceReport"/> class.
        /// </summary>
        /// <param name="sequenceReporting">The sequence reporting object.</param>
        public SequenceReport(ISequenceReporting sequenceReporting)
        {
            if (sequenceReporting == null)
            {
                throw new ArgumentNullException("sequenceReporting");
            }

            this.sequenceReporting = sequenceReporting;
        }

        /// <summary>
        /// Gets the width of the bitmap.
        /// </summary>
        /// <value>
        /// The width.
        /// </value>
        public long Width
        {
            get
            {
                if (this.seqMap24BppRgb != null)
                {
                    return this.seqMap24BppRgb.LongCount() / 3;
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets the bitmap as a single scan-line in 24bpp RGB format.
        /// </summary>
        /// <value>
        /// The bitmap.
        /// </value>
        public byte[] Bitmap
        {
            get
            {
                return this.seqMap24BppRgb;
            }
        }

        /// <summary>
        /// Initializes this sequence report.
        /// </summary>
        public void Initialize()
        {
            this.Initialize(this.sequenceReporting.Written);
        }

        /// <summary>
        /// Updates this sequence report.
        /// </summary>
        public void Update()
        {
            BitVector written = this.sequenceReporting.Written;
            if (written == null)
            {
                return;
            }

            if (written.LongCount != this.Width)
            {
                this.Initialize(written);
            }

            long lastSeq = this.seqNum;
            this.WriteColor(this.seqNum, this.waveNum, Palette[this.waveNum % Palette.Length]);
            this.waveNum = this.sequenceReporting.WaveNumber;
            this.seqNum = this.sequenceReporting.SequenceNumber;
            for (long i = lastSeq; lastSeq < this.seqNum; ++i)
            {
                if (written[i] && !this.writtenVector[i])
                {
                    this.writtenVector[i] = true;
                    this.WriteColor(i, this.waveNum, Palette[this.waveNum % Palette.Length]);
                }
            }

            this.WriteColor(this.seqNum, this.waveNum, SeqColor);
        }

        internal void Initialize(BitVector written)
        {
            this.seqMap24BppRgb = new byte[3 * written.LongCount];
            this.seqNum = this.waveNum = 0;
            this.writtenVector = new BitVector(written.LongCount);
        }

        private void WriteColor(long seq, long wave, int col)
        {
            byte r = (byte)((col & 0xFF) >> 16);
            byte g = (byte)((col & 0xFF) >> 8);
            byte b = (byte)(col & 0xFF);
            this.seqMap24BppRgb[wave * 3] = r;
            this.seqMap24BppRgb[(wave * 3) + 1] = g;
            this.seqMap24BppRgb[(wave * 3) + 2] = b;
        }
    }
}
