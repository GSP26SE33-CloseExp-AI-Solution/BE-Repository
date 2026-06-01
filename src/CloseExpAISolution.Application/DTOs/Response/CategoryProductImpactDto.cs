namespace CloseExpAISolution.Application.DTOs.Response;

public class CategoryProductImpactDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SubcategoryCount { get; set; }
    public int TotalProducts { get; set; }
    public int PublishedProducts { get; set; }
}
