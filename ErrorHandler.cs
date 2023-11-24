using Cosmos.HAL;
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
            Console.WriteLine($"[===== CRITICAL ERROR =====]\nERROR MSG: {Msg}\nERROR CODE: {ErrCode}");
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write("Press any key to reboot.");
            Cosmos.System.PCSpeaker.Beep(Cosmos.System.Notes.FS5, Cosmos.System.Durations.Half);

            int Second = RTC.Second;

            while (!Console.KeyAvailable)
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
            }

            Cosmos.System.Power.Reboot();
        }
        #endregion
    }
}
