using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Models;

public class RecommendationRequest
{
    [JsonPropertyName("query_text")]
    public string QueryText { get; set; } = string.Empty;
}
