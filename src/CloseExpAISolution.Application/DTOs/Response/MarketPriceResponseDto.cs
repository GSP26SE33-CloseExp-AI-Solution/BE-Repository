namespace CloseExpAISolution.Application.DTOs.Response;

public class MarketPriceResult
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AvgPrice { get; set; }
    public int SourceCount { get; set; }
    public List<string> Sources { get; set; } = new();
    public List<MarketPriceDetail> Details { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class MarketPriceDetail
{
    public string Source { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsInStock { get; set; }
    public DateTime CollectedAt { get; set; }
}

public class CrawlResult
{
    public bool Success { get; set; }
    public int PricesFound { get; set; }
    public List<string> Sources { get; set; } = new();
    public string? Error { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? AvgPrice { get; set; }
}
