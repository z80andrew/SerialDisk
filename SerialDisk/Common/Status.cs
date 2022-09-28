using System.Collections.Generic;

namespace Z80andrew.SerialDisk.Common
{
    public static class Status
    {
        public enum StatusKey
        {
            Idle,
            Stopped,
            Listening,
            Sending,
            Receiving,
            Writing,
            Reading,
            OperationComplete,
            Error
        }

        public static List<KeyValuePair<StatusKey, string>> Statuses
        {
            get
            {
                var statusList = new List<KeyValuePair<StatusKey, string>>
                {
                    new KeyValuePair<StatusKey, string>(StatusKey.Idle, "Idle"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Stopped, "Stopped"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Listening, "Listening"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Sending, "Sending"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Receiving, "Receiving"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Writing, "Writing"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Reading, "Reading"),
                    new KeyValuePair<StatusKey, string>(StatusKey.OperationComplete, "Completed"),
                    new KeyValuePair<StatusKey, string>(StatusKey.Error, "Error")
                };

                return statusList;
            }
        }
    }
}
