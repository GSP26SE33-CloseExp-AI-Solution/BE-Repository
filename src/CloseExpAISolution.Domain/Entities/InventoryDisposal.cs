namespace CloseExpAISolution.Domain.Entities;

public class InventoryDisposal
{
    public Guid DisposalId { get; set; }
    public Guid LotId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DestroyedBy { get; set; } = string.Empty;
    public DateTime DestroyedAt { get; set; }

    public StockLot? StockLot { get; set; }
}
