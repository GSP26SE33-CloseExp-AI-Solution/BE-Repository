namespace CloseExpAISolution.Domain.Entities;

public class ProductImage
{
    public Guid ProductImageId { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPrimary { get; set; }
    public Product? Product { get; set; }
}

