using System.Text;
using System.Text.Json;

public interface IChatSummarizer
{
    Task<string> SummarizeDataAsync(string inputText = "Long text to summarize...");
}
internal class OpenAISummarizer : IChatSummarizer
{
    private static string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={Environment.GetEnvironmentVariable("GEMINI_KEY")}";
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly ILogger<OpenAISummarizer> logger;

    public OpenAISummarizer(ILogger<OpenAISummarizer> logger)
    {
        this.logger = logger;
    }

    public async Task<string> SummarizeDataAsync(string inputText = "Long text to summarize...")
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = inputText }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, requestContent);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Error while summarizing data: " + response.ReasonPhrase);

            string responseContent = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseContent);
            var text = doc.RootElement
                          .GetProperty("candidates")[0]
                          .GetProperty("content")
                          .GetProperty("parts")[0]
                          .GetProperty("text")
                          .GetString();

            return text ?? "No summary found in response.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred.");
            return "Error ||| Could not Summarize";
        }
    }
}
