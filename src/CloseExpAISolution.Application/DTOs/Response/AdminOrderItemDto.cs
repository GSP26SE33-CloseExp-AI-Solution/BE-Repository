namespace CloseExpAISolution.Application.DTOs.Response;

public class AdminOrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid LotId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public string? PurchaseUnitSymbol { get; set; }
    public decimal? PurchaseQuantity { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? ProductName { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
