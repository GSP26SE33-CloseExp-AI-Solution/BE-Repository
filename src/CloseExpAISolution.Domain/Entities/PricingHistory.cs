namespace CloseExpAISolution.Domain.Entities;

public class PricingHistory
{
    public Guid AIPriceId { get; set; }
    public Guid LotId { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal? MarketMinPrice { get; set; }
    public decimal? MarketMaxPrice { get; set; }
    public decimal? MarketAvgPrice { get; set; }
    public decimal AIConfidence { get; set; }
    public string? Reason { get; set; }
    public string? Source { get; set; }
    public bool AcceptedSuggestion { get; set; }
    public string? Feedback { get; set; }
    public string? RejectionReason { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? SupermarketStaffId { get; set; }
    public Guid? SupermarketId { get; set; }
    public decimal? MarketPriceRef { get; set; }

    public DateTime CreatedAt { get; set; }

    public StockLot? StockLot { get; set; }
    public Supermarket? SupermarketRef { get; set; }
}
