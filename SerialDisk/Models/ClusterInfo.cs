using Z80andrew.SerialDisk.Common;

namespace Z80andrew.SerialDisk.Models
{
    public class ClusterInfo
    {
        public LocalDirectoryContentInfo LocalDirectoryContent;

        private readonly int _bytesPerCluster;
        public long FileOffset;
        private byte[] _dataBuffer;

        public bool IsDirectoryCluster => FileOffset == Constants.DirectoryClusterOffset;
        
        public ClusterInfo(int bytesPerCluster)
        {
            _bytesPerCluster = bytesPerCluster;
        }

        public byte[] DataBuffer
        {
            get
            {
                if (_dataBuffer == null) _dataBuffer = new byte[_bytesPerCluster];
                return _dataBuffer;
            }
        }

        public void DeallocateBuffer()
        {
            _dataBuffer = null;
        }
    }
}
