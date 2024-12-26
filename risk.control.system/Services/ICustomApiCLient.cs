using System.Net;
using System.Text.Json;

using Newtonsoft.Json.Linq;

namespace risk.control.system.Services
{
    public interface ICustomApiCLient
    {
        Task<(string Latitude, string Longitude)> GetCoordinatesFromAddressAsync(string address);
        Task<string> GetAddressFromLatLong(double latitude, double longitude);
        Task<(string distance, float distanceInMetres, string duration, int durationInSeconds, string map)> GetMap(double startLat, double startLong, double endLat, double endLong, string startLbl = "S", string endLbl = "E", string mapHeight = "300", string mapWidth = "200", string startColor = "red", string endColor = "green");
    }
    public class CustomApiClient : ICustomApiCLient
    {
        private static readonly string geocodeUrl = "https://maps.googleapis.com/maps/api/geocode/json";

        private static HttpClient client = new HttpClient();
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

        public async Task<(string distance, float distanceInMetres, string duration, int durationInSeconds, string map)> GetMap(double startLat, double startLong, double endLat, double endLong, string startLbl = "S", string endLbl = "E", string mapHeight = "300", string mapWidth = "200", string startColor = "red", string endColor = "green")
        {
            try
            {
                var driving = await GetDrivingDistance((startLat.ToString() +","+ startLong.ToString()),(endLat.ToString() +","+ endLong.ToString()));
                string directionsUrl = $"https://maps.googleapis.com/maps/api/directions/json?origin={startLat},{startLong}&destination={endLat},{endLong}&mode=driving&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                var response = await client.GetStringAsync(directionsUrl);
                var route = ParseRoute(response);
                string encodedPolyline = WebUtility.UrlEncode(route); // URL-encode the polyline
                var distanceMap = $"https://maps.googleapis.com/maps/api/staticmap?size={mapHeight}x{mapWidth}&markers=color:{startColor}|label:{startLbl}|{startLat},{startLong}&markers=color:{endColor}|label:{endLbl}|{endLat},{endLong}&path=enc:{encodedPolyline}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
                return (driving.Distance, driving.DistanceInMetres, driving.Duration, driving.DurationInTime, distanceMap);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return (null,0, null,0, null);
            }
            
        }
        static string ParseRoute(string directionsJson)
        {
            try
            {
                // Parse the JSON response
                var json = System.Text.Json.JsonDocument.Parse(directionsJson);

                // Check if the "routes" array exists and has at least one route
                if (json.RootElement.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                {
                    // Check if "overview_polyline" exists and has a "points" property
                    var firstRoute = routes[0];
                    if (firstRoute.TryGetProperty("overview_polyline", out var overviewPolyline) &&
                        overviewPolyline.TryGetProperty("points", out var points))
                    {
                        return points.GetString();
                    }
                    else
                    {
                        throw new Exception("Missing 'overview_polyline' or 'points' in the first route.");
                    }
                }
                else
                {
                    throw new Exception("No routes found in the Directions API response.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing route: {ex.Message}");
                return null; // Return null to indicate failure
            }
        }
        static async Task<(string Distance, float DistanceInMetres, string Duration, int DurationInTime)> GetDrivingDistance(string origin, string destination)
        {
            string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origin}&destinations={destination}&mode=driving&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            if (root.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
            {
                var firstRow = rows[0];
                if (firstRow.TryGetProperty("elements", out var elements) && elements.GetArrayLength() > 0)
                {
                    var firstElement = elements[0];
                    if (firstElement.GetProperty("status").GetString() == "OK")
                    {
                        var distance = firstElement.GetProperty("distance").GetProperty("text").GetString();
                        var distanceInMetre = float.Parse(firstElement.GetProperty("distance").GetProperty("value").ToString());
                        var duration = firstElement.GetProperty("duration").GetProperty("text").GetString();
                        var durationInSeconds = int.Parse(firstElement.GetProperty("duration").GetProperty("value").ToString());
                        return (distance, distanceInMetre, duration, durationInSeconds);
                    }
                }
            }

            return ("N/A", 0, "N/A", 0);
        }
    }
}
