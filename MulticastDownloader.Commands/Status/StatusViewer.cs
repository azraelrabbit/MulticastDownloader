// <copyright file="StatusViewer.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands.Status
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Core;
    using Properties;

    /// <summary>
    /// A simple multicast status viewer.
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Form" />
    public partial class StatusViewer : Form
    {
        private ITransferReporting transferReporting;
        private ISequenceReporting sequenceReporting;
        private IReceptionReporting receptionReporting;
        private SequenceReport sequenceReport;
        private TimeSpan updateInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusViewer"/> class.
        /// </summary>
        /// <param name="transferReporting">The transfer reporting interface.</param>
        /// <param name="sequenceReporting">The sequence reporting interface.</param>
        /// <param name="receptionReporting">The reception reporting interface.</param>
        /// <param name="updateInterval">The update interval.</param>
        public StatusViewer(ITransferReporting transferReporting, ISequenceReporting sequenceReporting, IReceptionReporting receptionReporting, TimeSpan updateInterval)
        {
            if (transferReporting == null)
            {
                throw new ArgumentNullException("transferReporting");
            }

            if (sequenceReporting == null)
            {
                throw new ArgumentNullException("sequenceReporting");
            }

            if (receptionReporting == null)
            {
                throw new ArgumentNullException("receptionReporting");
            }

            this.InitializeComponent();
            this.transferReporting = transferReporting;
            this.sequenceReporting = sequenceReporting;
            this.receptionReporting = receptionReporting;
            this.sequenceReport = new SequenceReport(this.sequenceReporting);
            this.updateInterval = updateInterval;
        }

        /// <summary>
        /// Processes a command key.
        /// </summary>
        /// <param name="msg">A <see cref="T:System.Windows.Forms.Message" />, passed by reference, that represents the Win32 message to process.</param>
        /// <param name="keyData">One of the <see cref="T:System.Windows.Forms.Keys" /> values that represents the key to process.</param>
        /// <returns>
        /// true if the keystroke was processed and consumed by the control; otherwise, false to allow further processing.
        /// </returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void StatusViewer_Load(object sender, EventArgs e)
        {
            this.sequenceReport.Initialize();
            this.UpdateTimer.Interval = (int)this.updateInterval.TotalMilliseconds;
            this.UpdateTimer.Enabled = true;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            this.Sequence.Text = this.sequenceReporting.SequenceNumber.ToString(CultureInfo.InvariantCulture);
            this.Wave.Text = this.sequenceReporting.WaveNumber.ToString(CultureInfo.InvariantCulture);
            this.ReceiveRate.Text = ((int)(this.receptionReporting.ReceptionRate * 100)).ToString(CultureInfo.CurrentCulture) + Resources.PercentSign;
            this.BytesPerSecond.Text = this.transferReporting.BytesPerSecond.ToString(CultureInfo.CurrentCulture);
            long bytesRemaining = this.transferReporting.BytesRemaining;
            this.BytesRemaining.Text = bytesRemaining.ToString(CultureInfo.CurrentCulture);
            long totalBytes = this.transferReporting.TotalBytes;
            this.BytesTotal.Text = totalBytes.ToString(CultureInfo.CurrentCulture);
            if (totalBytes > 0)
            {
                this.DownloadProgress.Style = ProgressBarStyle.Continuous;
                this.DownloadProgress.Value = (int)((totalBytes - bytesRemaining) / totalBytes);
                this.sequenceReport.Update();
            }

            this.Invalidate();
        }

        private void StatusViewer_Paint(object sender, PaintEventArgs e)
        {
            if (this.sequenceReport.Width == 0)
            {
                return;
            }

            long step = this.BitmapPanel.Width / this.sequenceReport.Width;
            using (Graphics g = this.CreateGraphics())
            using (Bitmap bmpSource = new Bitmap(this.BitmapPanel.Width, 1, g))
            {
                int x = 0;
                Color color;
                for (long seq = 0; seq < this.sequenceReport.Width; seq += step)
                {
                    color = Color.FromArgb(
                        this.sequenceReport.Bitmap[seq],
                        this.sequenceReport.Bitmap[seq + 1],
                        this.sequenceReport.Bitmap[seq + 2]);
                    bmpSource.SetPixel(x, 0, color);
                    ++x;
                }

                color = Color.FromArgb(
                    this.sequenceReport.Bitmap[this.sequenceReport.Width - 3],
                    this.sequenceReport.Bitmap[this.sequenceReport.Width - 2],
                    this.sequenceReport.Bitmap[this.sequenceReport.Width - 1]);
                bmpSource.SetPixel(this.BitmapPanel.Width - 1, 0, color);

                Rectangle dstRec = new Rectangle(this.BitmapPanel.Top, this.BitmapPanel.Left, this.BitmapPanel.Width, this.BitmapPanel.Height);
                g.DrawImage(bmpSource, dstRec);
            }
        }
    }
}
