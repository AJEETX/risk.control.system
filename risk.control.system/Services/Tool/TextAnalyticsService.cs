using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace risk.control.system.Services.Tool;

public interface ITextAnalyticsService
{
    Task<string> AbstractiveSummarizeAsync(string content);
    Task<string> CategorizeDocumentAsync(string content);
}
internal class TextAnalyticsService : ITextAnalyticsService
{
    //private readonly string _huggingFaceApiUrl = "https://router.huggingface.co/v1/facebook/bart-large-cnn"; // Hugging Face endpoint for summarization
    private readonly IHttpClientFactory httpClientFactory;

    public TextAnalyticsService(IHttpClientFactory httpClientFactory)
    {
        this.httpClientFactory = httpClientFactory;
    }
    public async Task<string> AbstractiveSummarizeAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Summary not available.";
        }

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://router.huggingface.co/"); ;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("HUGING_FACE"));

            var requestBody = new
            {
                inputs = content
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);
            HttpContent httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await httpClient.PostAsync("hf-inference/models/facebook/bart-large-cnn", httpContent);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(responseBody);

                // Extract and return the summary text
                return result?[0]?["summary_text"] ?? "Summary not available.";
            }
            else
            {
                string errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Hugging Face API error: {response.StatusCode} - {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }

        return "Summary not available.";
    }

    /// <summary>
    /// Categorize a document based on its content using key phrases.
    /// </summary>
    /// <param name="content">The content of the document.</param>
    /// <returns>The category of the document.</returns>
    public async Task<string> CategorizeDocumentAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Uncategorized";
        }

        try
        {
            // Define categories and their associated keywords
            var categories = new[]
            {
            new { Category = "Finance", Keywords = new[] { "investment", "finance", "banking", "accounting", "budget", "profit", "equity", "dividend", "expenses", "financial statement", "capital", "ROI" } },
            new { Category = "Sales", Keywords = new[] { "sales", "marketing", "customer", "revenue", "product", "pricing", "promotion", "lead generation", "campaign", "pipeline", "B2B", "B2C" } },
            new { Category = "HR", Keywords = new[] { "recruitment", "hiring", "employee", "policy", "training", "onboarding", "payroll", "HRIS", "performance", "benefits", "diversity", "workforce" } },
            new { Category = "IT", Keywords = new[] { "technology", "software", "hardware", "network", "security", "development", "cloud", "data center", "infrastructure", "DevOps", "cybersecurity", "database", "AI", "ML" } },
            new { Category = "Management", Keywords = new[] { "management", "leadership", "strategy", "planning", "organization", "goal", "roadmap", "team building", "stakeholders", "KPIs", "decision making", "operations" } },
            new { Category = "Legal", Keywords = new[] { "law", "contract", "compliance", "policy", "regulation", "litigation", "intellectual property", "negotiation", "dispute", "arbitration", "risk management", "legal framework" } },
            new { Category = "Marketing", Keywords = new[] { "advertising", "campaign", "brand", "promotion", "content", "media", "SEO", "social media", "analytics", "target audience", "public relations", "creative" } }
        };

            // Match categories based on keyword frequency
            var categoryScores = new Dictionary<string, int>();

            foreach (var category in categories)
            {
                int matchCount = category.Keywords
                    .Count(keyword => content.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (matchCount > 0)
                {
                    categoryScores[category.Category] = matchCount;
                }
            }

            // Select the category with the highest score
            if (categoryScores.Count > 0)
            {
                return categoryScores
                    .OrderByDescending(kvp => kvp.Value)
                    .First()
                    .Key;
            }

            return "General"; // Default category if no match is found
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return "Uncategorized";
        }
    }

}
