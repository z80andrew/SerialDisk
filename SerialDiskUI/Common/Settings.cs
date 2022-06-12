using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Resources;

namespace SerialDiskUI.Common
{
    public static class Constants
    {
        public static string DefaultPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"serialdiskui.config");

        public static KeyValuePair<string, int>[] BaudRates
        {
            get
            {
                var items = new KeyValuePair<string, int>[]
                {
                    new KeyValuePair<string, int>("300",300),
                    new KeyValuePair<string, int>("600",600),
                    new KeyValuePair<string, int>("1200",1200),
                    new KeyValuePair<string, int>("2400",2400),
                    new KeyValuePair<string, int>("4800",4800),
                    new KeyValuePair<string, int>("9600",9600),
                    new KeyValuePair<string, int>("14400",14400),
                    new KeyValuePair<string, int>("19200",19200)
                };

                return items;
            }
        }

        public static KeyValuePair<string, int>[] DataBitz
        {
            get
            {
                var items = new KeyValuePair<string, int>[]
                {
                    new KeyValuePair<string, int>("4",4),
                    new KeyValuePair<string, int>("5",5),
                    new KeyValuePair<string, int>("6",6),
                    new KeyValuePair<string, int>("7",7),
                    new KeyValuePair<string, int>("8",8)
                };

                return items;
            }
        }

        public static KeyValuePair<string, Handshake>[] Handshakes
        {
            get
            {
                var items = new KeyValuePair<string, Handshake>[]
                {
                    new KeyValuePair<string, Handshake>("None",Handshake.None),
                    new KeyValuePair<string, Handshake>("Hardware (RTS)",Handshake.RequestToSend),
                    new KeyValuePair<string, Handshake>("Software (XON/XOFF)",Handshake.XOnXOff)
                };

                return items;
            }
        }

        public static KeyValuePair<string, Parity>[] Parities
        {
            get
            {
                var items = new KeyValuePair<string, Parity>[]
                {
                    new KeyValuePair<string, Parity>("None",Parity.None),
                    new KeyValuePair<string, Parity>("Odd",Parity.Odd),
                    new KeyValuePair<string, Parity>("Even",Parity.Even),
                };

                return items;
            }
        }

        public static KeyValuePair<string, StopBits>[] StopBitz
        {
            get
            {
                var items = new KeyValuePair<string, StopBits>[]
                {
                    new KeyValuePair<string, StopBits>("0",StopBits.None),
                    new KeyValuePair<string, StopBits>("1",StopBits.One),
                    new KeyValuePair<string, StopBits>("1.5",StopBits.OnePointFive),
                    new KeyValuePair<string, StopBits>("2",StopBits.Two),
                };

                return items;
            }
        }
    }
}
