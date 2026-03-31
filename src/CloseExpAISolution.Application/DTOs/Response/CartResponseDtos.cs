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
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
