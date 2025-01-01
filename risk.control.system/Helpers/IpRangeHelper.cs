using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
namespace risk.control.system.Helpers
{
    public class IpRangeHelper
    {
        public static void GetIpRange()
        {
            var localIp = GetLocalIpAddress();
            var subnetMask = GetSubnetMask(localIp);

            if (localIp == null || subnetMask == null)
            {
                Console.WriteLine("Unable to retrieve IP or Subnet Mask.");
                return;
            }

            Console.WriteLine($"Local IP Address: {localIp}");
            Console.WriteLine($"Subnet Mask: {subnetMask}");

            // Calculate the network address
            var ipBytes = localIp.GetAddressBytes();
            var maskBytes = subnetMask.GetAddressBytes();
            var networkBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
            }
            var networkAddress = new IPAddress(networkBytes);

            // Calculate the broadcast address
            var broadcastBytes = new byte[ipBytes.Length];
            for (int i = 0; i < ipBytes.Length; i++)
            {
                broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }
            var broadcastAddress = new IPAddress(broadcastBytes);

            Console.WriteLine($"Network Address: {networkAddress}");
            Console.WriteLine($"Broadcast Address: {broadcastAddress}");
            Console.WriteLine($"IP Range: {networkAddress} - {broadcastAddress}");
        }

        // Get the local IP address
        private static IPAddress GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
        }

        // Get the subnet mask for a given IP address
        private static IPAddress GetSubnetMask(IPAddress ipAddress)
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                        ipAddress.Equals(unicast.Address))
                    {
                        return unicast.IPv4Mask;
                    }
                }
            }
            return null;
        }
    }
}
