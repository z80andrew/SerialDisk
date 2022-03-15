using System.Collections.Generic;

namespace AtariST.SerialDisk.Common
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
            Error
        }

        public static List<KeyValuePair<StatusKey, string>> Statuses
        {
            get
            {
                var statusList = new List<KeyValuePair<StatusKey, string>>();
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Idle, "Idle"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Stopped, "Stopped"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Listening, "Listening"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Sending, "Sending"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Receiving, "Receiving"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Writing, "Writing"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Reading, "Reading"));
                statusList.Add(new KeyValuePair<StatusKey, string>(StatusKey.Error, "Error"));

                return statusList;
            }
        }
    }
}
