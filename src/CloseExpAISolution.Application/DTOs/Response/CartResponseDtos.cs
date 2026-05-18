namespace CloseExpAISolution.Application.DTOs.Response;

public class CartResponseDto
{
    public Guid CartId { get; set; }
    public Guid UserId { get; set; }
    public int TotalItems { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IEnumerable<CartItemResponseDto> Items { get; set; } = new List<CartItemResponseDto>();
}

public class CartItemResponseDto
{
    public Guid CartItemId { get; set; }
    public Guid LotId { get; set; }
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public string? PurchaseUnitSymbol { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public Guid SupermarketId { get; set; }
    public string? SupermarketName { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? UnitSymbol { get; set; }
    public decimal ConversionRate { get; set; } = 1m;
    public Guid ProductUnitId { get; set; }
    public string? ProductUnitName { get; set; }
    public string? ProductUnitSymbol { get; set; }
    public decimal ProductConversionRate { get; set; } = 1m;
    public DateTime ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
