using System.Net.Http;
using System.Text;

using Google.Cloud.Vision.V1;

using Newtonsoft.Json;

using risk.control.system.Models;

public interface IChatSummarizer
{
    Task<string> SummarizeObjectDataAsync(ClaimsInvestigation claimsInvestigation);

    Task<string> SummarizeDataAsync(ClaimsInvestigation claimsInvestigation, string inputText = "Long text to summarize...");
}
public class OpenAISummarizer : IChatSummarizer
{
    private readonly HttpClient _httpClient;

    public OpenAISummarizer()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> SummarizeObjectDataAsync(ClaimsInvestigation claimsInvestigation)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("OPEN_API")}");

            string inputText = FormatObjectData(claimsInvestigation);
            var requestData = new
            {
                model = "gpt-3.5-turbo",
                messages = new[] { new { role = "user", content = $"Summarize the following claim investigation information:\n{inputText}" } },
                max_tokens = 100
            };
            var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", requestContent);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Error while summarizing data: " + response.ReasonPhrase);

            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            return result.choices[0].message.content;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    public async Task<string> SummarizeDataAsync(ClaimsInvestigation claimsInvestigation, string inputText = "Long text to summarize...")
    {
        try
        {
            var product = new Product
            {
                Name = "Wireless Mouse",
                Description = "A high-precision wireless mouse with ergonomic design.",
                Price = 25.99m,
                Category = "Electronics"
            };

            string requestData = FormatObjectData(product);
            if (!string.IsNullOrEmpty(inputText))
            {
                requestData = inputText;
            }
            var requestContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Environment.GetEnvironmentVariable("HUGING_FACE")}");
            HttpResponseMessage response = await _httpClient.PostAsync("https://api-inference.huggingface.co/models/facebook/bart-large-cnn", requestContent);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Error while summarizing data: " + response.ReasonPhrase);

            string responseContent = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseContent);

            return result[0].summary_text;

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }
    static string FormatObjectData(ClaimsInvestigation claimsInvestigation)
    {
        return claimsInvestigation.ToString();
    }
    private string FormatObjectData(Product product)
    {
        // Customize the formatting based on your Product class
        return $"Name: {product.Name}\nPrice: {product.Price}\nDescription: {product.Description}";
    }
}
public class Product
{
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}
