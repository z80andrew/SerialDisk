using AtariST.SerialDisk.Common;
using AtariST.SerialDisk.Interfaces;
using AtariST.SerialDisk.Models;
using DynamicData;
using ReactiveUI;
using System;
using System.Text;

namespace SerialDiskUI.Services
{
    public class StatusService : ReactiveObject, IStatusService
    {
        private Status.StatusKey _status;
        public Status.StatusKey Status
        {
            get => _status;
            private set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        private string _statusWithMessage;
        public string StatusWithMessage
        {
            get => _statusWithMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusWithMessage, value);
        }

        private string _diskObjectName;

        private int _totalBytes;
        public int TotalBytes
        {
            get => _totalBytes;
            private set => this.RaiseAndSetIfChanged(ref _totalBytes, value);
        }

        private int _transferredBytes;
        public int TransferredBytes
        {
            get => _transferredBytes;
            private set => this.RaiseAndSetIfChanged(ref _transferredBytes, value);
        }

        public StatusService()
        {
            SetStatus(AtariST.SerialDisk.Common.Status.StatusKey.Stopped);
        }

        public void SetTransferProgress(int totalBytes, int receivedBytes)
        {
            TotalBytes = totalBytes;
            TransferredBytes = receivedBytes;
        }

        public void SetStatus(Status.StatusKey status, string message = null!)
        {
            Status = status;

            // Reset progressbar values if not transferring
            if (status != AtariST.SerialDisk.Common.Status.StatusKey.Sending
                || status != AtariST.SerialDisk.Common.Status.StatusKey.Receiving
                || status != AtariST.SerialDisk.Common.Status.StatusKey.Error)
            {
                TotalBytes = Int32.MaxValue;
                TransferredBytes = Int32.MinValue;
            }

            var statusString = new StringBuilder()
                .Append("Status: ")
                .Append(AtariST.SerialDisk.Common.Status.Statuses.Find(x => x.Key == status).Value);

            if (status == AtariST.SerialDisk.Common.Status.StatusKey.Reading ||
                status == AtariST.SerialDisk.Common.Status.StatusKey.Writing)
            {
                _diskObjectName = message;
            }

            else if (!String.IsNullOrEmpty(message)) statusString.Append(" ").Append(message);

            if (status == AtariST.SerialDisk.Common.Status.StatusKey.Receiving
                || status == AtariST.SerialDisk.Common.Status.StatusKey.Reading)
            {
                statusString.Append($" {_diskObjectName}");
            }

            StatusWithMessage = statusString.ToString();
        }

        internal void AddLogEntry(LogMessage logMessage)
        {
            StatusWithMessage = logMessage.Message;
        }
    }
}
