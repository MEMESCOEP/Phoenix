using Cosmos.System.Network.IPv4.UDP.DHCP;
using Cosmos.System.FileSystem.VFS;
using Cosmos.System.Network.Config;
using Cosmos.System.Network.IPv4;
using Cosmos.HAL;
using System;
using Sys = Cosmos.System;
using Phoenix.CMD.Logging;
using Phoenix.Information;

namespace Phoenix
{
    public class Kernel : Sys.Kernel
    {
        protected override void OnBoot()
        {
            Sys.Global.Init(GetTextScreen(), false, true, true, true);
        }

        protected override void BeforeRun()
        {
            try
            {
                Console.WriteLine($"\n\n\n[===== {OSInfo.Name} {OSInfo.Version} ({OSInfo.Copyright}) =====]");
                Logger.Print("Kernel loaded.", Logger.LogType.Information);

                // FS Initializiation
                Logger.Print("Initializing filesystem...", Logger.LogType.Information);
                Sys.FileSystem.CosmosVFS fs = new Sys.FileSystem.CosmosVFS();
                VFSManager.RegisterVFS(fs);

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
                        Logger.Print("The network configuration failed!", Logger.LogType.Error);
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
                Console.Write(OSInfo.Prompt);
                var Command = Console.ReadLine();

                HandleCommand(Command, null);
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
                    break;

                case "reboot":
                case "restart":
                case "reset":
                    Sys.Power.Reboot();
                    break;

                case "":
                default:
                    break;
            }
        }
    }
}
