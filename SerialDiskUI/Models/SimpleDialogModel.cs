using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDiskUI.Models
{
    public class SimpleDialogModel
    {
        public enum ReturnType
        {
            OK,
            Cancel,
            Undef
        }

        public ReturnType ReturnValue { get; set; }

        public SimpleDialogModel()
        {
            ReturnValue = ReturnType.Undef;
        }

        public SimpleDialogModel(ReturnType returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}
