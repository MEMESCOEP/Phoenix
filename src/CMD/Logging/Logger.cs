using Cosmos.HAL;
using System;

namespace Phoenix.CMD.Logging
{
    public class Logger
    {
        public enum LogType
        {
            None,
            Error,
            Warning,
            Information,
            Debug
        }

        // Print a message to the console with the specified log level
        public static void Print(string Msg, LogType Type)
        {
            if(Type == LogType.None)
            {
                Console.WriteLine(Msg);
            }
            else
            {
                Console.Write("[");

                switch (Type)
                {
                    case LogType.Information:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"INFO@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;

                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"WARN@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;

                    case LogType.Error:
                        Console.Write("[");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"ERROR@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;

                    case LogType.Debug:
                        Console.Write("[");
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.Write($"DEBUG@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;
                }

                Console.ResetColor();
                Console.WriteLine($"] >> {Msg}");
            }
        }
    }
}
