namespace Z80andrew.SerialDisk.SerialDiskUI.Models
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
