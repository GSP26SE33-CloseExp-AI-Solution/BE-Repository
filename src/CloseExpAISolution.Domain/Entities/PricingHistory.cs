namespace CloseExpAISolution.Domain.Entities;

public class PricingHistory
{
    public Guid AIPriceId { get; set; }

    public Guid LotId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedUnitPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public decimal? MarketMinPrice { get; set; }
    public decimal? MarketMaxPrice { get; set; }
    public decimal? MarketAvgPrice { get; set; }
    public float AIConfidence { get; set; }
    public string? Reason { get; set; }
    public string? Source { get; set; }
    public bool AcceptedSuggestion { get; set; }
    public string? StaffFeedback { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public StockLot? StockLot { get; set; }
}
