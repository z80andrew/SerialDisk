using System;

namespace AtariST.SerialDisk.Shared
{
    public static class Error
    {
        public static void Log(Exception exception, string message = "")
        {
            Console.WriteLine($"{DateTime.UtcNow}  {message}");
            Console.WriteLine(exception);
        }
    }
}
