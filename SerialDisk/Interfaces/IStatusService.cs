using Z80andrew.SerialDisk.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z80andrew.SerialDisk.Interfaces
{
    public interface IStatusService
    {
        Status.StatusKey Status { get; }
        string StatusWithMessage { get; }
        int TotalBytes { get; }
        int TransferredBytes { get; }
        void SetTransferProgress(int totalBytes, int transferredBytes);
        void SetStatus(Status.StatusKey status, string message = null);
    }
}
