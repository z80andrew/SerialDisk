using System;
using System.Diagnostics;
using System.Text;
using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Interfaces;

namespace Z80andrew.SerialDisk.SerialDiskCLI.Services
{
    public class StatusService : IStatusService
    {
        public Status.StatusKey Status { get; set; }
        public string StatusWithMessage { get; set; }
        public int TotalBytes { get; set; }
        public int TransferredBytes { get; set; }

        public StatusService()
        {
            Status = SerialDisk.Common.Status.StatusKey.Idle;
        }

        public void SetTransferProgress(int totalBytes, int receivedBytes)
        {
            TotalBytes = totalBytes;
            TransferredBytes = receivedBytes;

            string percentTransferred = (Convert.ToDecimal(receivedBytes) / totalBytes * 100).ToString("00.00");

            if (Status == SerialDisk.Common.Status.StatusKey.Receiving)
            {
                string formattedBytesReceived = receivedBytes.ToString().PadLeft(totalBytes.ToString().Length, '0');
                Console.Write($"\rReceived [{formattedBytesReceived} / {totalBytes}] bytes {percentTransferred}% ");
            }

            else
            {
                Console.Write($"\rSent [{receivedBytes.ToString("D" + totalBytes.ToString().Length)} / {totalBytes} Bytes] {percentTransferred}% ");
            }

            if (totalBytes == receivedBytes) Console.WriteLine();
        }

        public void SetStatus(Status.StatusKey status, string message = null)
        {
            var statusString = new StringBuilder()
                .Append("Status: ")
                .Append(SerialDisk.Common.Status.Statuses.Find(x => x.Key == status).Value);

            if (!string.IsNullOrEmpty(message))
                statusString.Append(" ").Append(message);

            StatusWithMessage = statusString.ToString();

            // Write to Debug console only, as logger writes to stdout
            Debug.WriteLine(StatusWithMessage);
        }
    }
}