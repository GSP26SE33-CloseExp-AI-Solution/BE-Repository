namespace CloseExpAISolution.Application.DTOs.Response;

public class CategoryProductListItemDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal FinalPrice { get; set; }
}
