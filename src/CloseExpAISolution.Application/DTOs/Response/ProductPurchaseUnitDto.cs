namespace CloseExpAISolution.Application.DTOs.Response;

public class ProductPurchaseUnitDto
{
    public Guid UnitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; } = 1m;
    public bool IsProductDefault { get; set; }
    public bool HasPublishedLot { get; set; }
}
