namespace CloseExpAISolution.Domain.Entities;

public class AIPriceHistory
{
    public Guid PriceHistoryId { get; set; }
    public Guid LotId { get; set; }
    
    // Pricing info
    public decimal OriginalPrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    
    // Market comparison
    public decimal? MarketMinPrice { get; set; }
    public decimal? MarketMaxPrice { get; set; }
    public decimal? MarketAvgPrice { get; set; }
    
    // AI info
    public float AIConfidence { get; set; }
    public string? Reason { get; set; }
    public string? Source { get; set; }
    
    // Confirmation
    public bool AcceptedSuggestion { get; set; }
    public string? StaffFeedback { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public ProductLot? ProductLot { get; set; }
}
