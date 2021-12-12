using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using System;
using System.Text;

namespace AtariST.SerialDiskCLI
{
    public class StatusService : IStatusService
    {
        private Status.StatusKey _currentStatus;

        public Status.StatusKey Status
        {
            get
            {
                return _currentStatus;
            }

            private set
            {
                _currentStatus = value;
            }
        }

        private string _statusWithMessage;

        public string StatusWithMessage
        {
            get
            {
                return _statusWithMessage;
            }

            private set
            {
                _statusWithMessage = value;
            }
        }

        public int TotalBytes => throw new NotImplementedException();

        public int TransferredBytes => throw new NotImplementedException();

        private bool ShowDateTime;

        public StatusService()
        {
            Status = SerialDisk.Common.Status.StatusKey.Idle;
            ShowDateTime = false;
        }

        public void SetTransferProgress(int totalBytes, int transferredBytes)
        {
            string percentTransferred = ((Convert.ToDecimal(transferredBytes) / totalBytes) * 100).ToString("00.00");

            if (Status == SerialDisk.Common.Status.StatusKey.Receiving)
            {
                string formattedBytesReceived = transferredBytes.ToString().PadLeft(totalBytes.ToString().Length, '0');
                Console.Write($"\rReceived [{formattedBytesReceived} / {totalBytes}] bytes {percentTransferred}% ");
            }

            else
            {
                Console.Write($"\rSent [{(transferredBytes).ToString("D" + totalBytes.ToString().Length)} / {totalBytes} Bytes] {percentTransferred}% ");
            }

            if (totalBytes == transferredBytes) Console.WriteLine();
        }

        public void SetStatus(Status.StatusKey status, string message = null)
        {
            var statusString = new StringBuilder()
                .Append(GetPrependText())
                .Append("Status: ")
                .Append(SerialDisk.Common.Status.Statuses.Find(x => x.Key == status).Value);

            if (!String.IsNullOrEmpty(message))
                statusString.Append(" ").Append(message);

            StatusWithMessage = statusString.ToString();

            Console.WriteLine(StatusWithMessage);
        }

        private string GetPrependText()
        {
            if (ShowDateTime) return $"{DateTime.Now}\t";
            else return null;
        }
    }
}