// <copyright file="CmdletExtensionMethods.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Commands
{
    using System.Management.Automation;
    using Core;
    using Properties;

    internal static class CmdletExtensionMethods
    {
        internal static void WriteTransferProgress(this Cmdlet cmdlet, int activityId, ITransferReporting transferReporting)
        {
            ProgressRecord record;
            record = new ProgressRecord(activityId, Resources.DownloadActivity, transferReporting + " " + Resources.DownloadStatus);
            record.PercentComplete = record.SecondsRemaining = -1;
            record.RecordType = ProgressRecordType.Processing;
            long totalBytes = transferReporting.TotalBytes;
            long bytesRemaining = transferReporting.BytesRemaining;
            long bytesPerSecond = transferReporting.BytesPerSecond;
            if (totalBytes > 0)
            {
                record.PercentComplete = (int)(100.0 * (double)(totalBytes - bytesRemaining) / (double)totalBytes);
                if (bytesPerSecond > 0)
                {
                    record.SecondsRemaining = (int)((double)bytesRemaining / (double)bytesPerSecond);
                }
            }

            cmdlet.WriteProgress(record);
        }

        internal static void WriteTransferProgressComplete(this Cmdlet cmdlet, int activityId, ITransferReporting transferReporting)
        {
            ProgressRecord record;
            record = new ProgressRecord(activityId, Resources.DownloadActivity, transferReporting + " " + Resources.DownloadStatus);
            record.PercentComplete = record.SecondsRemaining = -1;
            record.RecordType = ProgressRecordType.Completed;
            cmdlet.WriteProgress(record);
        }

        internal static void WriteTransferReception(this Cmdlet cmdlet, int activityId, IReceptionReporting receptionReporting)
        {
            ProgressRecord record;
            record = new ProgressRecord(activityId, Resources.ReceptionActivity, receptionReporting + " " + Resources.ReceptionStatus);
            record.SecondsRemaining = -1;
            record.RecordType = ProgressRecordType.Processing;
            double receptionRate = receptionReporting.ReceptionRate;
            record.PercentComplete = (int)(100.0 * receptionRate);
            cmdlet.WriteProgress(record);
        }

        internal static void WriteTransferReceptionComplete(this Cmdlet cmdlet, int activityId, IReceptionReporting receptionReporting)
        {
            ProgressRecord record;
            record = new ProgressRecord(activityId, Resources.ReceptionActivity, receptionReporting + " " + Resources.ReceptionStatus);
            record.PercentComplete = record.SecondsRemaining = -1;
            record.RecordType = ProgressRecordType.Completed;
            cmdlet.WriteProgress(record);
        }
    }
}
