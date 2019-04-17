using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Shared;
using System;
using System.IO;
using System.IO.Ports;
using static AtariST.SerialDisk.Shared.Constants;

namespace AtariST.SerialDisk.Common
{
    public static class ApplicationParameters
    {
        public const string localDirectoryParam = "--local-directory";
        public const string diskSizeParam = "--disk-size";
        public const string portParam = "--port";
        public const string baudRateParam = "--baud-rate";
        public const string stopBitsParam = "--stop-bits";
        public const string dataBitsParam = "--data-bits";
        public const string parityParam = "--parity";
        public const string handshakeParam = "--handshake";
        public const string verbosityParam = "--verbosity";
        public const string logFileNameParam = "--log-file";

        public static ApplicationSettings ParseParameters(string[] arguments)
        {
            ApplicationSettings applicationSettings = new ApplicationSettings
            {
                SerialSettings = new SerialPortSettings()
            };

            for (int argindex = 0; argindex < arguments.Length; argindex+=2)
            {
                if (argindex == arguments.Length - 1) SetParameter(localDirectoryParam, arguments[argindex], applicationSettings);

                else SetParameter(arguments[argindex], arguments[argindex + 1], applicationSettings);
            }

            // local directory parameter not supplied
            if (applicationSettings.LocalDirectoryName == null) SetParameter(localDirectoryParam, null, applicationSettings);

            return applicationSettings;
        }

        private static void SetParameter(string argumentName, string argumentValue, ApplicationSettings applicationSettings)
        {
            switch (argumentName.ToLowerInvariant())
            {
                case localDirectoryParam:
                    if(String.IsNullOrEmpty(argumentValue)) argumentValue = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    applicationSettings.LocalDirectoryName = argumentValue;
                    break;

                case verbosityParam:
                    applicationSettings.LoggingLevel = (LoggingLevel)Enum.Parse(typeof(LoggingLevel), argumentValue, true);
                    break;

                case diskSizeParam:
                    applicationSettings.DiskSizeMiB = ParseIntParam(argumentName, argumentValue);
                    break;

                case portParam:
                    applicationSettings.SerialSettings.PortName = argumentValue;
                    break;

                case baudRateParam:
                    applicationSettings.SerialSettings.BaudRate = ParseIntParam(argumentName, argumentValue);
                    break;

                case dataBitsParam:
                    applicationSettings.SerialSettings.DataBits = ParseIntParam(argumentName, argumentValue);
                    break;

                case parityParam:
                    switch(argumentValue)
                    {
                        case "n":
                            applicationSettings.SerialSettings.Parity = Parity.None;
                            break;

                        case "e":
                            applicationSettings.SerialSettings.Parity = Parity.Even;
                            break;

                        case "m":
                            applicationSettings.SerialSettings.Parity = Parity.Mark;
                            break;

                        case "o":
                            applicationSettings.SerialSettings.Parity = Parity.Odd;
                            break;

                        case "s":
                            applicationSettings.SerialSettings.Parity = Parity.Space;
                            break;

                        default:
                            ArgumentException argEx = new ArgumentException($"{argumentName} value is invalid.");
                            Console.WriteLine(argEx.Message);
                            throw argEx;
                    }
                    break;

                case stopBitsParam:
                    switch (argumentValue)
                    {
                        case "1":
                            applicationSettings.SerialSettings.StopBits = StopBits.One;
                            break;

                        case "1.5":
                            applicationSettings.SerialSettings.StopBits = StopBits.OnePointFive;
                            break;

                        case "2":
                            applicationSettings.SerialSettings.StopBits = StopBits.Two;
                            break;

                        case "n":
                            applicationSettings.SerialSettings.StopBits = StopBits.None;
                            break;
                        default:
                            ArgumentException argEx = new ArgumentException($"{argumentName} value is invalid.");
                            Console.WriteLine(argEx.Message);
                            throw argEx;
                    }
                    break;

                case handshakeParam:
                    switch (argumentValue)
                    {
                        case "none":
                            applicationSettings.SerialSettings.Handshake = Handshake.None;
                            break;

                        case "rts":
                            applicationSettings.SerialSettings.Handshake = Handshake.RequestToSend;
                            break;

                        case "rts-xon-xoff":
                            applicationSettings.SerialSettings.Handshake = Handshake.RequestToSendXOnXOff;
                            break;

                        case "xon-xoff":
                            applicationSettings.SerialSettings.Handshake = Handshake.XOnXOff;
                            break;
                        default:
                            ArgumentException argEx = new ArgumentException($"{argumentName} value is invalid.");
                            Console.WriteLine(argEx.Message);
                            throw argEx;
                    }
                    break;

                case logFileNameParam:
                    applicationSettings.LogFileName = argumentValue;
                    break;
            }        
        }

        private static int ParseIntParam(string argumentName, string argumentValue)
        {
            try
            {
                return int.Parse(argumentValue);
            }

            catch (FormatException formatEx)
            {
                Console.WriteLine($"{argumentName} must be a number.");
                throw formatEx;
            }
        }
    }
}
