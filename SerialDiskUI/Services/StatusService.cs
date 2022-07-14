using Z80andrew.SerialDisk.Common;
using Z80andrew.SerialDisk.Interfaces;
using Z80andrew.SerialDisk.Models;
using DynamicData;
using ReactiveUI;
using System;
using System.IO;
using System.Text;

namespace Z80andrew.SerialDisk.SerialDiskUI.Services
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
            SetStatus(Z80andrew.SerialDisk.Common.Status.StatusKey.Stopped);
        }

        public void SetTransferProgress(int totalBytes, int receivedBytes)
        {
            TotalBytes = totalBytes;
            TransferredBytes = receivedBytes;

            var statusString = new StringBuilder()
                .Append("Status: ")
                .Append(Z80andrew.SerialDisk.Common.Status.Statuses.Find(x => x.Key == Status).Value)
                .Append($" ({TransferredBytes}/{TotalBytes} bytes)");
            
            StatusWithMessage = statusString.ToString();
        }

        public void SetStatus(Status.StatusKey status, string message = null!)
        {
            Status = status;

            // Reset progressbar values if not transferring
            if (status != Z80andrew.SerialDisk.Common.Status.StatusKey.Sending
                || status != Z80andrew.SerialDisk.Common.Status.StatusKey.Receiving
                || status != Z80andrew.SerialDisk.Common.Status.StatusKey.Error)
            {
                TotalBytes = Int32.MaxValue;
                TransferredBytes = Int32.MinValue;
            }

            var statusString = new StringBuilder()
                .Append("Status: ")
                .Append(Z80andrew.SerialDisk.Common.Status.Statuses.Find(x => x.Key == status).Value);

            if (!String.IsNullOrEmpty(message)) statusString.Append(" ").Append(message);

            StatusWithMessage = statusString.ToString();
        }


    }
}
