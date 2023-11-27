using Cosmos.Core;
using Cosmos.HAL;
using Phoenix.CMD.Logging;
using System;
using System.Threading;

namespace Phoenix
{
    public class ErrorHandler
    {
        /* FUNCTIONS */
        #region FUNCTIONS
        public static void CriticalError(string Msg, string ErrCode)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            Console.Write(new string(' ', Console.WindowWidth * Console.WindowHeight));
            Console.SetCursorPosition(0, 0);
            Logger.Print($"[=============================== CRITICAL ERROR ===============================]ERROR MESSAGE: {Msg}\nERROR CODE: {ErrCode}", Logger.LogType.None);
            Console.SetCursorPosition(0, Console.WindowHeight - 1);

            int Second = RTC.Second;
            Console.Write("Please restart the server.");

            // F# 5 is equal to 750 Hz; Half duration is equal to 800 ms
            Cosmos.System.PCSpeaker.Beep(Cosmos.System.Notes.FS5, Cosmos.System.Durations.Half);

            while (true)
            {
                if ((RTC.Second - Second) > 5)
                {
                    Second = RTC.Second;

                    for (int i = 0; i < 3; i++)
                    {
                        Cosmos.System.PCSpeaker.Beep(Cosmos.System.Notes.FS5, Cosmos.System.Durations.Default);
                        Thread.Sleep(200);
                    }
                }

                Thread.Sleep(25);
            }
        }
        #endregion
    }
}
