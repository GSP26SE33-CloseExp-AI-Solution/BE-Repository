using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Models;

public class StructuredSearchCriteria
{
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("max_price")]
    public decimal? MaxPrice { get; set; }

    [JsonPropertyName("min_price")]
    public decimal? MinPrice { get; set; }

    [JsonPropertyName("max_days_to_expire")]
    public int? MaxDaysToExpire { get; set; }
}
