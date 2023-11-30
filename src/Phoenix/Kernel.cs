using Cosmos.System.Network.IPv4.UDP.DHCP;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Network.Config;
using Cosmos.System.Network.IPv4;
using Cosmos.System.FileSystem;
using Cosmos.HAL.BlockDevice;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System;
using Sys = Cosmos.System;
using Phoenix.Information;
using Phoenix.CMD.Logging;
using IL2CPU.API.Attribs;
using LibDotNetParser.CILApi;
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;
using libDotNetClr;
using Console = System.Console;
using LibDotNetParser;

/* NAMESPACES */
#region NAMESPACES
namespace Phoenix
{
    /* CLASSES */
    #region CLASSES
    public class Kernel : Sys.Kernel
    {
        /* VARIABLES */
        #region VARIABLES
        [ManifestResourceStream(ResourceName = "Phoenix.Art.Logo.bmp")]
        static byte[] LogoArray;

        string CurrentWorkingDirectory = string.Empty;
        Canvas LogoBMP = Image.FromBitmap(LogoArray);
        public static CosmosVFS fs;
        #endregion

        /* FUNCTIONS */
        #region FUNCTIONS
        public override void Start()
        {
            try
            {
                if (!mStarted)
                {
                    mStarted = true;
                    Bootstrap.Init();

                    // Global init
                    Sys.Global.Init(GetTextScreen(), true, true, true, true);

                    BeforeRun();

                    while (!mStopped)
                    {
                        Run();
                    }
                }
                else
                {
                    throw new Exception("The kernel has already been started. A kernel cannot be started twice.");
                }
            }
            catch(Exception ex)
            {
                ErrorHandler.CriticalError($"{ex.Message}", ex.HResult.ToString());
            }
        }

        protected override void BeforeRun()
        {
            try
            {
                Console.WriteLine($"\n\n\n[===== {OSInfo.Name} {OSInfo.Version} ({OSInfo.Copyright}) =====]");
                Logger.Print("Kernel loaded and console initialized.", Logger.LogType.Information);

                try
                {
                    Display ScreenCanvas = Display.GetDisplay(640, 480);
                    ScreenCanvas.DrawImage(0, 0, LogoBMP, true);
                    ScreenCanvas.DrawString(0, (int)LogoBMP.Height, $"{OSInfo.Name} {OSInfo.Version} ({OSInfo.Copyright})", default, PrismAPI.Graphics.Color.White);
                    ScreenCanvas.Update();
                    Thread.Sleep(1000);
                    ScreenCanvas.IsEnabled = false;
                }
                catch(Exception ex)
                {
                    Logger.Print($"Boot graphics skipped: {ex.Message}", Logger.LogType.Warning);
                }

                // Serial initialization
                Logger.Print("Initializing serial port COM1...", Logger.LogType.Information);
                SerialPort.Enable(COMPort.COM1, BaudRate.BaudRate115200);

                // FS Initializiation
                Logger.Print("Initializing filesystem...", Logger.LogType.Information);
                try
                {
                    if (BlockDevice.Devices.Count > 0)
                    {
                        fs = new CosmosVFS();
                        VFSManager.RegisterVFS(fs);
                        Logger.Print("Setting the current working directory...", Logger.LogType.Information);

                        foreach (var disk in fs.Disks)
                        {
                            if (string.IsNullOrEmpty(CurrentWorkingDirectory))
                            {
                                if (disk.Partitions.Count > 0)
                                {
                                    if (disk.Partitions[0].HasFileSystem && String.IsNullOrEmpty(disk.Partitions[0].RootPath) == false)
                                    {
                                        CurrentWorkingDirectory = disk.Partitions[0].RootPath;
                                        Directory.SetCurrentDirectory(CurrentWorkingDirectory);
                                        Logger.Print($"Working directory is: \"{CurrentWorkingDirectory}\"", Logger.LogType.Information);
                                    }
                                }

                                else
                                {
                                    Logger.Print("The main DriveIndex doesn't have a root path.", Logger.LogType.Warning);
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Print("No supported IDE FAT32/UDF formatted disks are installed.", Logger.LogType.Warning);
                    }
                }
                catch(Exception ex)
                {
                    Logger.Print($"Disk init error: {ex.Message}\n\nPress any key to continue.", Logger.LogType.Error);
                    Console.ReadKey();
                }

                // Enable interrupts
                Logger.Print("Enabling global interrupts...", Logger.LogType.Information);
                Global.EnableInterrupts();

                // NIC Initialization
                Logger.Print("Initializing NICs...", Logger.LogType.Information);

                if (NetworkDevice.Devices.Count > 0)
                {
                    foreach (var nic in NetworkDevice.Devices)
                    {
                        //nic != null
                        using (var xClient = new DHCPClient())
                        {
                            /** Send a DHCP Discover packet **/
                            //This will automatically set the IP config after DHCP response
                            xClient.SendDiscoverPacket();
                        }

                        if (NetworkConfiguration.CurrentNetworkConfig == null || NetworkConfiguration.CurrentAddress.ToString() == "0.0.0.0")
                        {
                            Logger.Print("DHCP autoconfig failed, fallng back to defaults.", Logger.LogType.Warning);
                            IPConfig.Enable(nic, new Address(192, 168, 1, Convert.ToByte(69 + NetworkDevice.Devices.IndexOf(nic))), new Address(255, 255, 255, 0), new Address(192, 168, 1, 254));
                        }

                        if (NetworkConfiguration.CurrentNetworkConfig == null)
                        {
                            Logger.Print("Network configuration failed!", Logger.LogType.Error);
                            break;
                        }

                        Logger.Print($"[== {nic.Name} ({nic.NameID}) CONFIGURATION ==]\nIP: {NetworkConfiguration.CurrentAddress.ToString()}\nSubnet: {NetworkConfiguration.CurrentNetworkConfig.IPConfig.SubnetMask.ToString()}\nDefault gateway: {NetworkConfiguration.CurrentNetworkConfig.IPConfig.DefaultGateway.ToString()}\nMAC: {nic.MACAddress.ToString()}\nAdapter: {nic.Name}\nID: {nic.NameID}\n", Logger.LogType.None);
                    }                    
                }
                else
                {
                    Logger.Print("There are no supported NICs installed.", Logger.LogType.Warning);
                }

                Logger.Print("Init done.", Logger.LogType.Information);
            }
            catch (Exception ex)
            {
                ErrorHandler.CriticalError($"{ex.Message}", ex.HResult.ToString());
            }
        }

        protected override void Run()
        {
            try
            {
                Console.Write($"({CurrentWorkingDirectory}) {OSInfo.Prompt}");
                var Input = Console.ReadLine();
                var Args = Input.Split(' ');

                HandleCommand(Args[0], Args);
            }
            catch (Exception ex)
            {
                ErrorHandler.CriticalError($"{ex.Message}", ex.HResult.ToString());
            }
        }

        void HandleCommand(string Command, string[] Arguments)
        {
            switch(Command)
            {
                /* POWER */
                case "shutdown":
                case "turnoff":
                case "poweroff":
                    Sys.Power.Shutdown();
                    ErrorHandler.CriticalError("Shutdown failed!", "-256");
                    break;

                case "reboot":
                case "restart":
                case "reset":
                    Sys.Power.Reboot();
                    ErrorHandler.CriticalError("Reboot failed!", "-255");
                    break;



                /* FILESYSTEM */
                case "cat":
                case "read":
                    Logger.Print(File.ReadAllText(Path.GetFullPath(Arguments[1])), Logger.LogType.None);
                    break;

                case "rm":
                case "del":
                    File.Delete(Path.GetFullPath(Arguments[1]));
                    break;

                case "cp":
                case "copy":
                case "duplicate":
                    File.Copy(Path.GetFullPath(Arguments[1]), Path.GetFullPath(Arguments[2]));
                    break;

                case "ls":
                case "dir":
                    Logger.Print($"[== {CurrentWorkingDirectory} ==]", Logger.LogType.None);

                    foreach (var entry in Directory.GetFiles(CurrentWorkingDirectory))
                    {
                        Logger.Print($"[FILE] {entry}", Logger.LogType.None);
                    }

                    foreach (var entry in Directory.GetDirectories(CurrentWorkingDirectory))
                    {
                        Logger.Print($"[DIR] {entry}", Logger.LogType.None);
                    }

                    Console.WriteLine();
                    break;

                case "cd":
                    var dir = "";

                    if (Arguments.Length <= 1 || string.IsNullOrWhiteSpace(Path.GetFullPath(Arguments[1])))
                    {
                        Logger.Print($"A directory name must be specified.\n", Logger.LogType.Error);
                        break;
                    }

                    if (Arguments[1].StartsWith("\""))
                    {
                        dir = "";

                        foreach (var part in Arguments)
                        {
                            dir += part + " ";
                        }

                        // Get the directory name inside of the quotes
                        int pFrom = dir.IndexOf("\"") + 1;
                        int pTo = dir.LastIndexOf("\"");
                        dir = dir.Substring(pFrom, pTo - pFrom);
                        dir.Replace($"{Command} ", "").TrimEnd();
                    }
                    else
                    {
                        dir = Path.GetFullPath(Arguments[1]);
                    }

                    if (Directory.Exists(dir) == false)
                    {
                        Logger.Print($"The directory \"{dir}\" does not exist.\n", Logger.LogType.Error);
                        break;
                    }

                    dir = Path.GetFullPath(dir);
                    Directory.SetCurrentDirectory(dir);
                    CurrentWorkingDirectory = dir;
                    break;

                case "format":
                    if(fs.Disks.Count <= 0)
                    {
                        Logger.Print("There are no disks to format.\n", Logger.LogType.Error);
                        break;
                    }

                    foreach(var disk in fs.Disks)
                    {
                        Console.WriteLine($"[== Disk #{fs.Disks.IndexOf(disk)} ==]");
                        disk.DisplayInformation();
                        Console.WriteLine();
                    }

                    Console.Write($"Choose a disk to format (0-{fs.Disks.Count}) >> ");
                    int Disk = Convert.ToInt32(Console.ReadLine());
                    int Partition = 0;
                    bool QuickFormat = false;

                    if(fs.Disks[Disk].Partitions.Count <= 0)
                    {
                        Console.WriteLine($"This disk does not have any partitions, so one will be created.");
                        Logger.Print($"Creating partition on disk #{Disk}...", Logger.LogType.Information);
                        fs.Disks[Disk].CreatePartition(fs.Disks[Disk].Size);
                        Logger.Print($"Partition created.", Logger.LogType.Information);
                    }
                    else
                    {
                        Console.Write($"Choose a partition to format (0-{fs.Disks[Disk].Partitions.Count}) >> ");
                        Partition = Convert.ToInt32(Console.ReadLine());

                        Console.Write($"Quick format? (Y/n) >> ");
                        QuickFormat = Console.ReadLine().ToLower() == "y";
                    }

                    Logger.Print($"Formatting disk #{Disk} (partition #{Partition}, Quick format: {QuickFormat})...", Logger.LogType.Information);
                    fs.Disks[Disk].FormatPartition(Partition, "FAT32", QuickFormat);
                    Logger.Print($"Formatting complete.\n", Logger.LogType.Information);
                    break;



                /* CONSOLE */
                case "clear":
                case "cls":
                    Console.Clear();
                    break;



                /* MISC */
                case "halt":
                case "hlt":
                    Logger.Print("The kernel has been halted.", Logger.LogType.Information);
                    mStopped = true;
                    break;

                case "":
                    break;

                default:
                    if (File.Exists(Path.GetFullPath(Arguments[0])))
                    {
                        DotNetFile DotNetFile = new DotNetFile(Path.GetFullPath(Arguments[0]));

                        foreach(var disk in fs.Disks)
                        {
                            foreach(var partition in disk.Partitions)
                            {
                                if (partition.HasFileSystem == false)
                                {
                                    continue;
                                }

                                var FrameworkPath = $"{partition.MountedFS.GetRootDirectory().mFullPath}\\framework\\";

                                if (Directory.Exists(FrameworkPath))
                                {
                                    DotNetClr CLR = new DotNetClr(DotNetFile, FrameworkPath);
                                    CLR.RegisterCustomInternalMethod("TestSuccess", TestSuccess);
                                    CLR.RegisterCustomInternalMethod("TestsComplete", TestsComplete);
                                    CLR.Start();

                                    //CLR.Start();
                                    goto StopSearchingDrives;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Print($"Invalid command: \"{Command}\"\n", Logger.LogType.Error);
                    }

                    StopSearchingDrives:;
                    break;
            }

            // Call the garbage collector
            Heap.Collect();
        }

        private static void TestSuccess(MethodArgStack[] Stack, ref MethodArgStack returnValue, DotNetMethod method)
        {
            Console.WriteLine($"{method.Parms[0].TypeInString} test succeeded");
        }

        private static void TestsComplete(MethodArgStack[] Stack, ref MethodArgStack returnValue, DotNetMethod method)
        {
            Console.WriteLine("All Tests Completed.");
        }
        #endregion

        /* COROUTINES */
        #region COROUTINES
        #endregion
    }
    #endregion
}
#endregion
