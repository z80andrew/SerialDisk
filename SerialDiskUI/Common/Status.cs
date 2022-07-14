using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z80andrew.SerialDisk.SerialDiskUI.Common
{
    public enum Status
    {
        Idle,
        Listening,
        Sending,
        Receiving,
        Error
    }
}
