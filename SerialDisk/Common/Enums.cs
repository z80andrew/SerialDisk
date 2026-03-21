using System;
using System.Collections.Generic;
using System.Text;

namespace Z80andrew.SerialDisk.Common
{
	public enum TOSVersion
	{
		TOS100,
		TOS104
	}

	public enum ReceiverState
	{
		ReceiveStartMagic = 0,
		ReceiveReadSectorIndex,
		ReceiveReadSectorCount,
		SendData,
		ReceiveWriteSectorIndex,
		ReceiveWriteSectorCount,
		ReceiveData,
		ReceiveCRC32,
		SendMediaChangeStatus,
		SendBiosParameterBlock
	};

	public enum LoggingLevel
	{
		Info = 0,
		Debug,
		All
	};
}
