using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using risk.control.system.Models.ViewModel;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace risk.control.system.Services
{
    public interface ISmsService
    {
        Task DoSendSmsAsync(string mobile, string message, bool onboard = false);
    }

    public class SmsService : ISmsService
    {
        private static HttpClient client = new HttpClient();
        private readonly IFeatureManager featureManager;

        public SmsService(IFeatureManager featureManager)
        {
            this.featureManager = featureManager;
        }

        public async Task DoSendSmsAsync(string mobile, string message, bool onboard = false)
        {
            if (await featureManager.IsEnabledAsync(FeatureFlags.SMS4ADMIN) || onboard)
            {
                await SendSmsAsync(mobile, message);
            }
        }

        public static async Task<string> SendSmsAsync(string mobile = "+61432854196", string message = "Testing fom Azy")
        {
            try
            {
                //var localIps = GetActiveIPAddressesInNetwork();
                var url = Environment.GetEnvironmentVariable("SMS_Url");

                var username = Environment.GetEnvironmentVariable("SMS_User");
                var password = Environment.GetEnvironmentVariable("SMS_Pwd");
                var sim = Environment.GetEnvironmentVariable("SMS_Sin") ?? "1";
                var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
                mobile = mobile.StartsWith("+") ? mobile : "+" + mobile;

                var newContent = new { message = message, phoneNumbers = new List<string> { mobile }, simNumber = int.Parse(sim) };
                var jsonContent = JsonConvert.SerializeObject(newContent);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);

                // Log the response
                Console.WriteLine($"Response Status Code: {response.StatusCode}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response: " + responseBody);
                response.EnsureSuccessStatusCode();
                return responseBody;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending SMS: " + ex.Message);
                return ex.Message;
            }
        }


        static List<string> GetActiveIPAddressesInNetwork()
        {
            List<string> activeIPs = new List<string>();

            string localIP = GetLocalIPAddress();
            if (string.IsNullOrEmpty(localIP))
            {
                return activeIPs; // Return empty if no local IP is found
            }

            // Get the network address and subnet mask for the local machine
            string[] localIPParts = localIP.Split('.');
            if (localIPParts.Length != 4)
            {
                return activeIPs; // Return empty if the IP format is invalid
            }

            // Let's assume the subnet mask is 255.255.255.0 (standard for many local networks)
            string subnetMask = GetSubnetMask();
            string networkPrefix = $"{localIPParts[0]}.{localIPParts[1]}.{localIPParts[2]}.";

            // Start scanning from 1 to 254 (common range in 255.255.255.0 subnet)
            for (int i = 1; i <= 254; i++)
            {
                string ipAddressToCheck = networkPrefix + i;

                if (IsHostAlive(ipAddressToCheck))
                {
                    activeIPs.Add(ipAddressToCheck);
                }
            }

            return activeIPs;
        }

        static string GetSubnetMask()
        {
            string subnetMask = string.Empty;

            try
            {
                // Get all network interfaces on the machine
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var netInterface in networkInterfaces)
                {
                    // Ensure the interface is up and operational
                    if (netInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        // Get the IP properties of the interface
                        IPInterfaceProperties ipProperties = netInterface.GetIPProperties();

                        // Loop through the UnicastAddresses to find IPv4 addresses
                        foreach (var address in ipProperties.UnicastAddresses)
                        {
                            // Check if the address is IPv4
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                // Get the subnet mask
                                subnetMask = address.IPv4Mask.ToString();
                                return subnetMask; // Return the first found subnet mask
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., no network interface found)
                Console.WriteLine(ex.Message);
            }

            return subnetMask;
        }
        static List<string> GetLocalIPAddresses()
        {
            List<string> ipAddresses = new List<string>();

            try
            {
                // Get all network interfaces on the machine
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var netInterface in networkInterfaces)
                {
                    // Get the IP properties of the network interface
                    IPInterfaceProperties ipProperties = netInterface.GetIPProperties();

                    // Get the list of unicast IP addresses (IPv4 and IPv6)
                    foreach (var address in ipProperties.UnicastAddresses)
                    {
                        // Filter out loopback addresses (127.x.x.x, ::1)
                        if (!IPAddress.IsLoopback(address.Address))
                        {
                            ipAddresses.Add(address.Address.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception if needed (e.g., log it)
                Console.WriteLine(ex.Message);
            }

            return ipAddresses;
        }
        // Function to check if a host (IP address) is alive using Ping
        private static bool IsHostAlive(string ipAddress)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(ipAddress, 1000); // Timeout in 1000 ms
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                return false; // If an exception occurs, consider the IP as inactive
            }
        }
        static string GetLocalIPAddress()
        {
            string localIP = string.Empty;

            try
            {
                // Get the host name of the current machine
                string hostName = Dns.GetHostName();

                // Get the list of IP addresses associated with the host name
                IPAddress[] addresses = Dns.GetHostAddresses(hostName);

                // Loop through the addresses and find the IPv4 address
                foreach (var address in addresses)
                {
                    // Check if it's an IPv4 address (you could skip IPv6 addresses here)
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIP = address.ToString();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exception if needed (e.g., log it)
                Console.WriteLine(ex.Message);
            }

            return localIP;
        }
    }
}