using System.Text;
using System.Text.Json;
using risk.control.system.Helpers;

namespace risk.control.system.Services.Common
{
    public interface ITinyUrlService
    {
        Task<string> ShortenUrlAsync(string longUrl);
    }

    internal class TinyUrlService(IHttpClientFactory httpClientFactory) : ITinyUrlService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        public async Task<string> ShortenUrlAsync(string longUrl)
        {
            var request = new
            {
                url = longUrl,
                domain = "tiny.one"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://api.tinyurl.com/");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {EnvHelper.Get("TINY_URL_KEY")}");

            var response = await httpClient.PostAsync("create", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            return doc.RootElement
                      .GetProperty("data")
                      .GetProperty("tiny_url")
                      .GetString()!;
        }
    }
}