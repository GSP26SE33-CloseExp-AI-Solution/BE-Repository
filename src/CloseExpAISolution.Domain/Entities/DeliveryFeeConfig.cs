namespace CloseExpAISolution.Domain.Entities;

public class DeliveryFeeConfig
{
    public Guid ConfigId { get; set; }
    public decimal MinDistance { get; set; }
    public decimal MaxDistance { get; set; }
    public decimal BaseFee { get; set; }
    public decimal FeePerKm { get; set; }
    public string? Area { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
