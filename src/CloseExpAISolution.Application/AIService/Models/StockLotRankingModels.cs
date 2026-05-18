using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Models;

public class StockLotInputDto
{
    [JsonPropertyName("lot_id")]
    public string? LotId { get; set; }

    [JsonPropertyName("product_id")]
    public string? ProductId { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit_name")]
    public string? UnitName { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("expiry_date")]
    public DateTime ExpiryDate { get; set; }

    [JsonPropertyName("manufacture_date")]
    public DateTime? ManufactureDate { get; set; }
}

public class RankStockLotsRequest
{
    [JsonPropertyName("query_text")]
    public string? QueryText { get; set; }

    [JsonPropertyName("stocklots")]
    public List<StockLotInputDto>? StockLots { get; set; }
}

public class RankedStockLotDto
{
    [JsonPropertyName("lot_id")]
    public string? LotId { get; set; }

    [JsonPropertyName("relevance_score")]
    public decimal RelevanceScore { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class RankStockLotsResponse
{
    [JsonPropertyName("ranked_stocklots")]
    public List<RankedStockLotDto>? RankedStockLots { get; set; }

    [JsonPropertyName("total_ranked")]
    public int TotalRanked { get; set; }
}
