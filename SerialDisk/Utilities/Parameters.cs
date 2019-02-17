using AtariST.SerialDisk.Models;
using AtariST.SerialDisk.Shared;
using System;
using System.IO.Ports;

namespace AtariST.SerialDisk.Utilities
{
    public static class Parameters
    {
        private const string localDirectoryParam = "--local-directory";
        private const string diskSizeParam = "--disk-size";
        private const string portParam = "--port";
        private const string baudRateParam = "--baud-rate";
        private const string stopBitsParam = "--stop-bits";
        private const string dataBitsParam = "--data-bits";
        private const string parityParam = "--parity";
        private const string flowControlParam = "--handshake";
        private const string verbosityParam = "--verbosity";

        public static Settings ParseParameters(string[] arguments)
        {
            Settings applicationSettings = new Settings
            {
                SerialSettings = new SerialPortSettings()
            };

            for (int argindex = 0; argindex < arguments.Length; argindex++)
            {
                if (argindex == arguments.Length - 1) SetParameter(localDirectoryParam, arguments[argindex], applicationSettings);
                else SetParameter(arguments[argindex], arguments[argindex + 1], applicationSettings);
            }

            return applicationSettings;
        }

        private static void SetParameter(string argumentName, string argumentValue, Settings applicationSettings)
        {
            switch (argumentName.ToLowerInvariant())
            {
                case localDirectoryParam:
                    applicationSettings.LocalDirectoryName = argumentValue;
                    break;

                case verbosityParam:
                    applicationSettings.Verbosity = ParseIntParam(argumentName, argumentValue);
                    break;

                case diskSizeParam:
                    applicationSettings.DiskSizeMB = ParseIntParam(argumentName, argumentValue);
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
                            Error.Log(argEx, argEx.Message);
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
                            Error.Log(argEx, argEx.Message);
                            throw argEx;
                    }
                    break;

                case flowControlParam:
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
                            Error.Log(argEx, argEx.Message);
                            throw argEx;
                    }
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
                Error.Log(formatEx, $"{argumentName} must be a number.");
                throw formatEx;
            }
        }
    }
}
