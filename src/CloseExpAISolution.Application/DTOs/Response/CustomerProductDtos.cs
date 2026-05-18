namespace CloseExpAISolution.Application.DTOs.Response;

public class CustomerProductListItemDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public string Brand { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public int AvailableLotCount { get; set; }
    public decimal MinSellingUnitPrice { get; set; }
    public DateTime NearestExpiryDate { get; set; }
    public string? MainImageUrl { get; set; }
}

public class CustomerStockLotOptionDto
{
    public Guid LotId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime ManufactureDate { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal SellingUnitPrice { get; set; }
    public string UnitName { get; set; } = string.Empty;
}

public class CustomerProductDetailDto
{
    public Guid ProductId { get; set; }
    public Guid SupermarketId { get; set; }
    public string SupermarketName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsFreshFood { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Origin { get; set; }
    public string? MainImageUrl { get; set; }
    public List<ProductImageDto> ProductImages { get; set; } = new();
    public decimal TotalAvailableQuantity { get; set; }
    public decimal MinSellingUnitPrice { get; set; }
    public DateTime NearestExpiryDate { get; set; }
    public List<CustomerStockLotOptionDto> AvailableStockLots { get; set; } = new();
}
