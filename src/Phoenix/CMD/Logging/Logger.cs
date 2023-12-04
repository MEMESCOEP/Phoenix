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
            if(Type == LogType.Debug)
            {
                if (Kernel.IsDebug)
                { 
                    Console.Write("[");
                    SerialPort.SendString($"[");

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write($"DEBUG@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                    SerialPort.SendString($"DEBUG@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");

                    Console.ResetColor();
                    Console.WriteLine($"] >> {Msg}");
                    SerialPort.SendString($"] >> {Msg}\n");
                }
            }
            else if (Type == LogType.None)
            {
                Console.WriteLine(Msg);
                SerialPort.SendString($"{Msg}\n");
            }
            else
            {
                Console.Write("[");
                SerialPort.SendString($"[");

                switch (Type)
                {
                    case LogType.Information:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"INFO@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        SerialPort.SendString($"INFO@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;

                    case LogType.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"WARN@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        SerialPort.SendString($"WARN@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;

                    case LogType.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"ERROR@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        SerialPort.SendString($"ERROR@{RTC.Hour}:{RTC.Minute}:{RTC.Second}");
                        break;
                }

                Console.ResetColor();
                Console.WriteLine($"] >> {Msg}");
                SerialPort.SendString($"] >> {Msg}\n");
            }
        }
    }
}
