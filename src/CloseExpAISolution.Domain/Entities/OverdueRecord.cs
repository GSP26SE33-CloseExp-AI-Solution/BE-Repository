namespace CloseExpAISolution.Domain.Entities;

public class OverdueRecord
{
    public Guid OverdueId { get; set; }
    public Guid LotId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string DestroyedBy { get; set; } = string.Empty;
    public DateTime DestroyedAt { get; set; }

    public ProductLot? ProductLot { get; set; }
}
