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
            switch (Type)
            {
                case LogType.None:
                    Console.WriteLine(Msg);
                    break;

                case LogType.Information:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("INFO");
                    Console.ResetColor();
                    Console.WriteLine($"] >> {Msg}");
                    break;

                case LogType.Warning:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("WARN");
                    Console.ResetColor();
                    Console.WriteLine($"] >> {Msg}");
                    break;

                case LogType.Error:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR");
                    Console.ResetColor();
                    Console.WriteLine($"] >> {Msg}");
                    break;

                case LogType.Debug:
                    Console.Write("[");
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.Write("DEBUG");
                    Console.ResetColor();
                    Console.WriteLine($"] >> {Msg}");
                    break;
            }
        }
    }
}
