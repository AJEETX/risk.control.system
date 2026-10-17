using System.Text.Json.Serialization;

namespace risk.control.system.Models.ViewModel
{
    public record LoginRequest(string Userid, string Password);
    public record AiResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("expires_at")] DateTime ExpiresAt
);
    public record StartAdjudicationRequest(string Mode, string Provider_id);
    public record AdjudicationResponse(long Job_id, string Progress_url, string Status_url);
}
