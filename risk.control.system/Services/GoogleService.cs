using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace risk.control.system.Services;

public interface IGoogleService
{
    Task<List<string>> GetAutocompleteSuggestions(string input, string types = "address");
}
internal class GoogleService : IGoogleService
{
    private static HttpClient client = new HttpClient();
    private readonly ILogger<GoogleService> logger;

    public GoogleService(ILogger<GoogleService> logger)
    {
        this.logger = logger;
    }
    public async Task<List<string>> GetAutocompleteSuggestions(string input, string types = "address")
    {
        // Google Places Autocomplete API endpoint
        string endpoint = "https://maps.googleapis.com/maps/api/place/autocomplete/json";

        // Construct the URL with the input query, types, and API key
        string url = $"{endpoint}?input={input}&types={types}&key={Environment.GetEnvironmentVariable("GOOGLE_MAP_KEY")}";
        try
        {
            // Send the request to the Places API
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                // Parse the response body (JSON format)
                string responseBody = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<JObject>(responseBody);
                var predictionData = new List<string>();
                // Extract and print the autocomplete predictions
                var predictions = json["predictions"];
                if (predictions.HasValues)
                {
                    Console.WriteLine("Autocomplete Suggestions:");
                    foreach (var prediction in predictions)
                    {
                        string description = prediction["description"].ToString();
                        predictionData.Add(description);
                        Console.WriteLine(description);
                    }
                }
                else
                {
                    Console.WriteLine("No autocomplete suggestions found.");
                }
                return predictionData;
            }
            else
            {
                Console.WriteLine("Error fetching data: " + response.ReasonPhrase);
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred.");
            throw;
        }
    }
}
