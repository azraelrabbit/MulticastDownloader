namespace MS.MulticastDownloader.Commands.Status
{
    partial class StatusViewer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Forms.Control.set_Text(System.String)", Justification = "none")]
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.DownloadProgress = new System.Windows.Forms.ProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.BytesPerSecond = new System.Windows.Forms.Label();
            this.BytesRemaining = new System.Windows.Forms.Label();
            this.BytesTotal = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.Sequence = new System.Windows.Forms.Label();
            this.ReceiveRate = new System.Windows.Forms.Label();
            this.Wave = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.BitmapPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // UpdateTimer
            // 
            this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
            // 
            // DownloadProgress
            // 
            this.DownloadProgress.Location = new System.Drawing.Point(30, 546);
            this.DownloadProgress.Margin = new System.Windows.Forms.Padding(7, 9, 7, 9);
            this.DownloadProgress.Name = "DownloadProgress";
            this.DownloadProgress.Size = new System.Drawing.Size(2150, 108);
            this.DownloadProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.DownloadProgress.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 500);
            this.label1.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(124, 37);
            this.label1.TabIndex = 1;
            this.label1.Text = "Progress:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 689);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(223, 37);
            this.label2.TabIndex = 2;
            this.label2.Text = "Bytes Per Second:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(832, 689);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(218, 37);
            this.label3.TabIndex = 3;
            this.label3.Text = "Bytes Remaining:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1597, 689);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(149, 37);
            this.label4.TabIndex = 4;
            this.label4.Text = "Bytes Total:";
            // 
            // BytesPerSecond
            // 
            this.BytesPerSecond.AutoSize = true;
            this.BytesPerSecond.Location = new System.Drawing.Point(252, 689);
            this.BytesPerSecond.Name = "BytesPerSecond";
            this.BytesPerSecond.Size = new System.Drawing.Size(32, 37);
            this.BytesPerSecond.TabIndex = 5;
            this.BytesPerSecond.Text = "0";
            // 
            // BytesRemaining
            // 
            this.BytesRemaining.AutoSize = true;
            this.BytesRemaining.Location = new System.Drawing.Point(1056, 689);
            this.BytesRemaining.Name = "BytesRemaining";
            this.BytesRemaining.Size = new System.Drawing.Size(32, 37);
            this.BytesRemaining.TabIndex = 6;
            this.BytesRemaining.Text = "0";
            // 
            // BytesTotal
            // 
            this.BytesTotal.AutoSize = true;
            this.BytesTotal.Location = new System.Drawing.Point(1743, 689);
            this.BytesTotal.Name = "BytesTotal";
            this.BytesTotal.Size = new System.Drawing.Size(32, 37);
            this.BytesTotal.TabIndex = 7;
            this.BytesTotal.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 440);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 37);
            this.label5.TabIndex = 8;
            this.label5.Text = "Sequence:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(1908, 500);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(172, 37);
            this.label6.TabIndex = 9;
            this.label6.Text = "Receive Rate:";
            // 
            // Sequence
            // 
            this.Sequence.AutoSize = true;
            this.Sequence.Location = new System.Drawing.Point(155, 440);
            this.Sequence.Name = "Sequence";
            this.Sequence.Size = new System.Drawing.Size(32, 37);
            this.Sequence.TabIndex = 10;
            this.Sequence.Text = "0";
            // 
            // ReceiveRate
            // 
            this.ReceiveRate.AutoSize = true;
            this.ReceiveRate.Location = new System.Drawing.Point(2086, 500);
            this.ReceiveRate.MaximumSize = new System.Drawing.Size(94, 37);
            this.ReceiveRate.MinimumSize = new System.Drawing.Size(94, 37);
            this.ReceiveRate.Name = "ReceiveRate";
            this.ReceiveRate.Size = new System.Drawing.Size(94, 37);
            this.ReceiveRate.TabIndex = 11;
            this.ReceiveRate.Text = "0%";
            this.ReceiveRate.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // Wave
            // 
            this.Wave.AutoSize = true;
            this.Wave.Location = new System.Drawing.Point(2148, 440);
            this.Wave.Name = "Wave";
            this.Wave.Size = new System.Drawing.Size(32, 37);
            this.Wave.TabIndex = 13;
            this.Wave.Text = "0";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(1992, 440);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(88, 37);
            this.label8.TabIndex = 12;
            this.label8.Text = "Wave:";
            // 
            // BitmapPanel
            // 
            this.BitmapPanel.BackColor = System.Drawing.Color.Black;
            this.BitmapPanel.Location = new System.Drawing.Point(12, 13);
            this.BitmapPanel.Name = "BitmapPanel";
            this.BitmapPanel.Size = new System.Drawing.Size(2186, 424);
            this.BitmapPanel.TabIndex = 14;
            // 
            // StatusViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 37F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2210, 772);
            this.Controls.Add(this.BitmapPanel);
            this.Controls.Add(this.Wave);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.ReceiveRate);
            this.Controls.Add(this.Sequence);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.BytesTotal);
            this.Controls.Add(this.BytesRemaining);
            this.Controls.Add(this.BytesPerSecond);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.DownloadProgress);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 10.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(7, 9, 7, 9);
            this.MaximizeBox = false;
            this.Name = "StatusViewer";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Multicast Status Viewer";
            this.Load += new System.EventHandler(this.StatusViewer_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.StatusViewer_Paint);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer UpdateTimer;
        private System.Windows.Forms.ProgressBar DownloadProgress;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label BytesPerSecond;
        private System.Windows.Forms.Label BytesRemaining;
        private System.Windows.Forms.Label BytesTotal;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label Sequence;
        private System.Windows.Forms.Label ReceiveRate;
        private System.Windows.Forms.Label Wave;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Panel BitmapPanel;
    }
}