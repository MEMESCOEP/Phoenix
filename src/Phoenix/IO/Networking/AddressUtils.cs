using Cosmos.System.Network.IPv4;
using System.Linq;
using System;

namespace Phoenix.IO.Networking
{
    public class AddressUtils
    {
        public static bool IsIPv4AddressValid(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        public static Address StringToAddress(string IPAddress)
        {
            try
            {
                return Address.Parse(IPAddress);
            }
            catch
            {
                return new Address(0, 0, 0, 0);
            }
        }
    }
}
