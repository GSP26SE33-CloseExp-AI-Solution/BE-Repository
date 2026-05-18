namespace CloseExpAISolution.Application.DTOs.Request;

public class CustomerProductQueryRequestDto
{
    public Guid? SupermarketId { get; set; }
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public bool? IsFreshFood { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool SortPriceAsc { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
