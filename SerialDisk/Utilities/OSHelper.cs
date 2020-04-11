using System.Runtime.InteropServices;

namespace AtariST.SerialDisk.Utilities
{
    public static class OSHelper
    {
        public static string OperatingSystemName
        {
            get
            {
                string osName = string.Empty;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) osName = OSPlatform.Windows.ToString();
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) osName = OSPlatform.Linux.ToString();
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) osName = OSPlatform.OSX.ToString();

                return osName;
            }
        }
    }
}
