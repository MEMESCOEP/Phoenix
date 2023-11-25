using Cosmos.System.Network.IPv4.UDP.DHCP;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Network.Config;
using Cosmos.System.Network.IPv4;
using Cosmos.Core.Memory;
using Cosmos.HAL;
using System.IO;
using System;
using Sys = Cosmos.System;
using Phoenix.Information;
using Phoenix.CMD.Logging;

namespace Phoenix
{
    public class Kernel : Sys.Kernel
    {
        /* VARIABLES */
        #region VARIABLES
        string CurrentWorkingDirectory = string.Empty;
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

                // FS Initializiation
                Logger.Print("Initializing filesystem...", Logger.LogType.Information);
                try
                {
                    Sys.FileSystem.CosmosVFS fs = new Sys.FileSystem.CosmosVFS();
                    VFSManager.RegisterVFS(fs);

                    if (fs.Disks.Count > 0)
                    {
                        Logger.Print("Setting the current working directory...", Logger.LogType.Information);

                        foreach (var disk in fs.Disks)
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
                                Logger.Print("The main disk doesn't have a root path.", Logger.LogType.Warning);
                            }
                        }
                    }
                    else
                    {
                        Logger.Print("No functioning FAT32/UDF formatted disks are installed, You won't be able to save anything.\n\nPress any key to continue.", Logger.LogType.Warning);
                        Console.ReadKey();
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
                NetworkDevice nic = NetworkDevice.GetDeviceByName("eth0");

                if (nic != null)
                {
                    using (var xClient = new DHCPClient())
                    {
                        /** Send a DHCP Discover packet **/
                        //This will automatically set the IP config after DHCP response
                        xClient.SendDiscoverPacket();
                    }

                    if (NetworkConfiguration.CurrentNetworkConfig == null || NetworkConfiguration.CurrentAddress.ToString() == "0.0.0.0")
                    {
                        Logger.Print("DHCP autoconfig failed, fallng back to defaults.", Logger.LogType.Warning);
                        IPConfig.Enable(nic, new Address(192, 168, 1, 69), new Address(255, 255, 255, 0), new Address(192, 168, 1, 254));
                    }

                    if (NetworkConfiguration.CurrentNetworkConfig == null)
                    {
                        Logger.Print("Network configuration failed!", Logger.LogType.Error);
                    }

                    Logger.Print($"[== NETWORK CONFIGURATION ==]\nIP: {NetworkConfiguration.CurrentAddress.ToString()}\nSubnet: {NetworkConfiguration.CurrentNetworkConfig.IPConfig.SubnetMask.ToString()}\nDefault gateway: {NetworkConfiguration.CurrentNetworkConfig.IPConfig.DefaultGateway.ToString()}\nMAC: {nic.MACAddress.ToString()}\nAdapter: {nic.Name}\nID: {nic.NameID}\n", Logger.LogType.None);
                }
                else
                {
                    Logger.Print("There are no supported NICs installed.", Logger.LogType.Warning);
                }
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

                case "ls":
                case "dir":
                    Logger.Print($"[== {CurrentWorkingDirectory} ==]", Logger.LogType.None);

                    foreach(var entry in Directory.GetFiles(CurrentWorkingDirectory))
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
                    CurrentWorkingDirectory = Path.GetFullPath(Arguments[1]);
                    Directory.SetCurrentDirectory(CurrentWorkingDirectory);
                    break;

                case "":
                    break;

                default:
                    Logger.Print($"Invalid command: \"{Command}\"\n", Logger.LogType.Error);
                    break;
            }

            // Call the garbage collector
            Heap.Collect();
        }
        #endregion
    }
}
