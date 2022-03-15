using AtariST.SerialDisk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtariST.SerialDisk.Interfaces
{
    public interface IStatusService
    {
        public Status.StatusKey Status { get; }
        public string StatusWithMessage { get; }
        public string DiskObject { get; }
        public int TotalBytes { get; }
        public int TransferredBytes { get; }
        void SetTransferProgress(int totalBytes, int transferredBytes);
        void SetStatus(Status.StatusKey status, string message = null);
    }
}
