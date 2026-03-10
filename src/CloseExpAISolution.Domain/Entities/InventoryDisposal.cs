namespace CloseExpAISolution.Domain.Entities;

/// <summary>
/// Inventory disposal record (per ER: renamed from OverdueRecord).
/// One record per lot disposal with DestroyID (PK), LotID (FK), Reason, DestroyedBy, DestroyedAt.
/// </summary>
public class InventoryDisposal
{
    public Guid DestroyId { get; set; }
    public Guid LotId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DestroyedBy { get; set; } = string.Empty;
    public DateTime DestroyedAt { get; set; }

    public ProductLot? ProductLot { get; set; }
}
