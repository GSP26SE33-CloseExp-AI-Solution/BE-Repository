namespace CloseExpAISolution.Domain.Entities;

public class AIPriceHistory
{
    public Guid AIPriceId { get; set; }
    public Guid LotId { get; set; }
    public decimal MarketMinPrice { get; set; }
    public decimal MarketMaxPrice { get; set; }
    public decimal MarketAvgPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ProductLot? ProductLot { get; set; }
}
