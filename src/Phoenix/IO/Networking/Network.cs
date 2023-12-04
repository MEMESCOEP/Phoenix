using Cosmos.System.Network.IPv4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Phoenix.IO.Networking
{
    public class Network
    {
        public static int ICMPPing(Address IPAddress, int Timeout = 5000)
        {
            Cosmos.System.Network.IPv4.EndPoint endPoint = new(IPAddress, 0);

            using (var xClient = new ICMPClient())
            {
                xClient.Connect(endPoint.Address);

                // Send an ICMP Echo message
                xClient.SendEcho();

                // Receive ICMP Response (return elapsed time / timeout if no response)
                int time = xClient.Receive(ref endPoint, Timeout);
                return time;
            }
        }
    }
}
