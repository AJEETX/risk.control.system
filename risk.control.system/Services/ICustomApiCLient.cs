using Newtonsoft.Json.Linq;

namespace risk.control.system.Services
{
    public interface ICustomApiCLient
    {
        Task<(string Latitude, string Longitude)> GetCoordinatesFromAddressAsync(string address);
        Task<string> GetAddressFromLatLong(double latitude, double longitude);
    }
    public class CustomApiClient : ICustomApiCLient
    {
        private static readonly string geocodeUrl = "https://maps.googleapis.com/maps/api/geocode/json";

        public async Task<(string Latitude, string Longitude)> GetCoordinatesFromAddressAsync(string address)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    // Google Geocoding API endpoint
                    string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

                    // Send the GET request
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    // Read the response content as a string
                    string content = await response.Content.ReadAsStringAsync();

                    // Parse the response JSON
                    JObject jsonResponse = JObject.Parse(content);

                    // Check if the status is OK
                    if (jsonResponse["status"].ToString() == "OK")
                    {
                        // Extract latitude and longitude
                        var location = jsonResponse["results"][0]["geometry"]["location"];
                        string latitude = location["lat"].ToObject<string>();
                        string longitude = location["lng"].ToObject<string>();

                        return (latitude.ToString(), longitude.ToString());
                    }
                    else
                    {
                        Console.WriteLine($"Error: {jsonResponse["status"]}");
                        return ("0", "0"); // Return 0,0 if the request was unsuccessful
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return ("0", "0"); // Return 0,0 if the request was unsuccessful
            }
        }

        public async Task<string> GetAddressFromLatLong(double latitude, double longitude)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    // Construct the request URL
                    var requestUrl = $"{geocodeUrl}?latlng={latitude},{longitude}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

                    // Make the HTTP GET request to the Google Geocoding API
                    var response = await client.GetStringAsync(requestUrl);

                    // Parse the JSON response
                    var jsonResponse = JObject.Parse(response);

                    // Check if the response contains results
                    if (jsonResponse["status"].ToString() == "OK")
                    {
                        // Get the formatted address from the response
                        var address = jsonResponse["results"][0]["formatted_address"].ToString();
                        return address;
                    }
                    else
                    {
                        return "No address found for the given coordinates.";
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
        }
    }
}
