namespace AtariST.SerialDisk.Shared
{
    public static class Constants
    {
        public enum ReceiverState
        {
            ReceiveStartMagic = 0,
            ReceiveCommand,
            ReceiveReadSectorIndex,
            ReceiveReadSectorCount,
            SendReadData,
            SendReadCrc32,
            ReceiveWriteSectorIndex,
            ReceiveWriteSectorCount,
            ReceiveWriteData,
            SendWriteCrc32,
            SendMediaChangeStatus,
            SendBiosParameterBlock,
            ReceiveEndMagic
        };

        public enum LoggingLevel
        {
            Verbose = 0,
            Info,
            Warn,
            Error
        };
    }
}
