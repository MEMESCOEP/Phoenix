// Cosmos
using Cosmos.System.Network.IPv4.UDP.DHCP;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Network.Config;
using Cosmos.System.Network.IPv4;
using Cosmos.System.FileSystem;
using Cosmos.HAL.BlockDevice;
using Cosmos.Core.Memory;
using Cosmos.Core;
using Cosmos.HAL;

// System
using System.Threading;
using System.IO;
using System;

// Cosmos system
using Sys = Cosmos.System;

// Phoenix classes (memescoep)
using Phoenix.Information;
using Phoenix.CMD.Logging;

// IL2CPU
using IL2CPU.API.Attribs;

// PrismAPI (Terminal.cs)
using PrismAPI.Hardware.GPU;
using PrismAPI.Graphics;

// DOtNetParser (MishaProductions)
using LibDotNetParser.CILApi;
using LibDotNetParser;
using libDotNetClr;
using Phoenix.IO.Networking;

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
        // Arays
        [ManifestResourceStream(ResourceName = "Phoenix.Art.Logo.bmp")]
        static byte[] LogoArray;

        // Strings
        public static string CurrentWorkingDirectory = string.Empty;
        public static string FrameworkPath = String.Empty;

        // Canvases
        Canvas LogoBMP = Image.FromBitmap(LogoArray);

        // Floats
        public static float TotalInstalledRAM = 0f;
        public static float UsedRAM = 0f;

        // Virtual filesystems
        public static CosmosVFS fs;

        // Date & time
        public static DateTime KernelStartTime;

        // Booleans
        public static bool IsDebug = false;
        #endregion

        /* FUNCTIONS */
        #region FUNCTIONS
        public override void Start()
        {
            try
            {
                KernelStartTime = DateTime.Now;

                if (!mStarted)
                {
                    mStarted = true;
                    Cosmos.HAL.Bootstrap.Init();

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
                TotalInstalledRAM = CPU.GetAmountOfRAM() * 1024;

                /*try
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
                }*/

                // Serial initialization
                Logger.Print("Initializing serial port COM1...", Logger.LogType.Information);
                SerialPort.Enable(COMPort.COM1, BaudRate.BaudRate9600);

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
                                        break;
                                    }
                                }

                                else
                                {
                                    Logger.Print($"Drive #{fs.Disks.IndexOf(disk)} doesn't have a filesystem or a root path.", Logger.LogType.Warning);
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

                // Find the DotNetParser framework
                Logger.Print($"Searching for the DotNetParser framework on local storage...", Logger.LogType.Information);

                if (string.IsNullOrEmpty(CurrentWorkingDirectory) == false)
                {
                    foreach (var disk in fs.Disks)
                    {
                        foreach (var partition in disk.Partitions)
                        {
                            if (partition.HasFileSystem == false)
                            {
                                continue;
                            }

                            FrameworkPath = $"{partition.MountedFS.GetRootDirectory().mFullPath}\\framework\\";

                            if (Directory.Exists(FrameworkPath))
                            {
                                Logger.Print($"Framework path is: \"{FrameworkPath}\"", Logger.LogType.Information);
                                goto StopSearchingForFramework;
                            }
                        }
                    }
                }

                Logger.Print($"Failed to find DotNetParser framework.", Logger.LogType.Error);
                FrameworkPath = string.Empty;

                StopSearchingForFramework:;

                // Enable interrupts
                Logger.Print("Enabling global interrupts...", Logger.LogType.Information);
                Cosmos.HAL.Global.EnableInterrupts();

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
                // Update the memory usage variable
                Logger.Print($"Calculating used RAM amount...", Logger.LogType.Debug);
                UsedRAM = GCImplementation.GetUsedRAM() / 1024;

                Console.Write($"(");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(CurrentWorkingDirectory);
                Console.ResetColor();
                Console.Write($") {OSInfo.Prompt}");

                var Input = Console.ReadLine();
                var Args = Input.Split(' ');

                Logger.Print($"Handling command (\"{Args[0]}\")...", Logger.LogType.Debug);
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

                case "ed":
                case "edit":
                case "change":
                    if(Arguments.Length > 1)
                    {
                        MIV.StartMIV(Path.GetFullPath(Arguments[1]));
                    }
                    else
                    {
                        MIV.StartMIV(null);
                    }

                    break;

                case "rm":
                case "del":
                    if (Arguments[1] == "-rf")
                    {
                        Directory.Delete(Path.GetFullPath(Arguments[2]));
                    }

                    File.Delete(Path.GetFullPath(Arguments[1]));
                    break;

                case "cp":
                case "copy":
                case "duplicate":
                    File.Copy(Path.GetFullPath(Arguments[1]), Path.GetFullPath(Arguments[2]));
                    break;

                case "mv":
                case "move":
                case "transfer":
                    File.Copy(Path.GetFullPath(Arguments[1]), Path.GetFullPath(Arguments[2]));
                    File.Delete(Path.GetFullPath(Arguments[1]));
                    break;

                case "mkf":
                    File.Create(Path.GetFullPath(Arguments[1]));
                    break;

                case "mkdir":
                    Directory.CreateDirectory(Path.GetFullPath(Arguments[1]));
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
                    Logger.Print($"Checking drive count...", Logger.LogType.Debug);
                    if (fs.Disks.Count <= 0)
                    {
                        Logger.Print("There are no disks to format.\n", Logger.LogType.Error);
                        break;
                    }

                    Logger.Print($"Displaying disk information...", Logger.LogType.Debug);
                    foreach (var disk in fs.Disks)
                    {
                        Console.WriteLine($"[== Disk #{fs.Disks.IndexOf(disk)} ==]");
                        disk.DisplayInformation();
                        Console.WriteLine();
                    }

                    Console.Write($"Choose a disk to format (0-{fs.Disks.Count}) >> ");
                    int Disk = Convert.ToInt32(Console.ReadLine());
                    int Partition = 0;
                    bool QuickFormat = false;

                    Logger.Print($"Checking drive partition count...", Logger.LogType.Debug);
                    if (fs.Disks[Disk].Partitions.Count <= 0)
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
                    Logger.Print($"\nFormatting complete, restart the server for the changes to take effect.\n", Logger.LogType.Information);
                    break;



                /* NETWORK */
                case "ping":
                    if (Arguments.Length <= 1 || string.IsNullOrWhiteSpace(Arguments[1]))
                    {
                        Logger.Print($"An IP address must be specified.\n\n", Logger.LogType.Error);
                        break;
                    }

                    if (AddressUtils.IsIPv4AddressValid(Arguments[1]) == false)
                    {
                        Logger.Print($"\"{Arguments[1]}\" is not a valid IP address.\n\n", Logger.LogType.Error);
                        break;
                    }

                    Logger.Print($"Pinging {Arguments[1]}:", Logger.LogType.Information);
                    int SuccessCounter = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        float PingTime = Network.ICMPPing(AddressUtils.StringToAddress(Arguments[1]));

                        if (PingTime >= 0)
                        {
                            SuccessCounter++;
                            Logger.Print($"\t[{i + 1}] Ping succeeded in {PingTime} milliseconds.", Logger.LogType.None);
                            Thread.Sleep(250);
                        }

                        else
                        {
                            Logger.Print($"\tPing failed.", Logger.LogType.None);
                        }
                    }

                    Logger.Print($"{SuccessCounter}/4 pings succeeded. ({(float)(SuccessCounter / 4f) * 100f}%)\n", Logger.LogType.Information);
                    break;



                /* CONSOLE */
                case "clear":
                case "cls":
                    Console.Clear();
                    break;



                /* SYSTEM */
                case "sysinfo":
                case "systeminfo":
                case "sysinformation":
                case "systeminformation":
                    int CDCount = 0, HDDCount = 0, RVMCount = 0, OtherCount = 0;

                    Logger.Print($"Searching for drives...", Logger.LogType.Debug);
                    foreach (var drive in fs.Disks)
                    {
                        if(drive.Type == BlockDeviceType.HardDrive)
                        {
                            HDDCount++;
                        }
                        else if (drive.Type == BlockDeviceType.RemovableCD)
                        {
                            CDCount++;
                        }
                        else if (drive.Type == BlockDeviceType.Removable)
                        {
                            RVMCount++;
                        }
                        else
                        {
                            OtherCount++;
                        }
                    }

                    Logger.Print($"[===== {OSInfo.Name} {OSInfo.Version} ({OSInfo.Copyright}) =====]\n" +
                        $"Kernel start time: {KernelStartTime.ToString()}\n" +
                        $"System uptime: {(DateTime.Now - KernelStartTime).ToString()}\n" +
                        $"CPU: {CPU.GetCPUBrandString()}\n" +
                        $"CPU Vendor: {CPU.GetCPUVendorName()}\n" +
                        $"CPU Uptime: {CPU.GetCPUUptime()}\n" +
                        $"RAM: {TotalInstalledRAM} KB ({UsedRAM} KB used, {(UsedRAM / TotalInstalledRAM) * 100}%)\n" +
                        $"Installed drives: {fs.Disks.Count} (HDD: {HDDCount}, CD: {CDCount}, RVM: {RVMCount}, OTHER: {OtherCount})\n", Logger.LogType.None);
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

                        if (string.IsNullOrEmpty(FrameworkPath) == false && Directory.Exists(FrameworkPath))
                        {
                            Logger.Print($"Creating CLR...", Logger.LogType.Debug);
                            DotNetClr CLR = new DotNetClr(DotNetFile, FrameworkPath);

                            Logger.Print($"Registering internal CLR methods...", Logger.LogType.Debug);
                            CLR.RegisterCustomInternalMethod("TestSuccess", TestSuccess);
                            CLR.RegisterCustomInternalMethod("TestsComplete", TestsComplete);

                            Logger.Print($"Starting program execution...", Logger.LogType.Debug);
                            CLR.Start();

                            Logger.Print($"Program finished.", Logger.LogType.Debug);
                        }
                    }
                    else
                    {
                        Logger.Print($"Invalid command: \"{Command}\"\n", Logger.LogType.Error);
                    }

                    break;
            }

            // Call the garbage collector
            Logger.Print($"Calling the garbage collector and freeing pages...", Logger.LogType.Debug);
            int CollectedObjects = Heap.Collect();
            Logger.Print($"Freed {CollectedObjects} objects", Logger.LogType.Debug);

            // Heap.Collect leaves behind empty SMT pages, so we'll need to clean them up
            int FreedPages = HeapSmall.PruneSMT();
            Logger.Print($"Pruned {FreedPages} pages", Logger.LogType.Debug);

            // Some debug stuff
            Logger.Print($"The garbage collector was called {RAT.GCTriggered} times", Logger.LogType.Debug);
            Logger.Print($"Free pages: {RAT.FreePageCount}", Logger.LogType.Debug);
            Logger.Print($"Min free pages: {RAT.MinFreePages}", Logger.LogType.Debug);
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
