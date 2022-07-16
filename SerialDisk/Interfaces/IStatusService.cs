using Z80andrew.SerialDisk.Common;

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
