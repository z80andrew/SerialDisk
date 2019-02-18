using System;

namespace AtariST.SerialDisk.Shared
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow} {message}");
        }

        public static void LogError(Exception exception, string message = "")
        {
            Console.WriteLine($"{DateTime.UtcNow}  {message}");
            Console.WriteLine(exception);
        }
    }
}
