namespace CloseExpAISolution.Application.DTOs.Response;

public class CategoryResponseDto
{
    public Guid CategoryId { get; set; }
    public Guid? ParentCatId { get; set; }
    public string? ParentName { get; set; }
    public bool IsFreshFood { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CatIconUrl { get; set; }
    public bool IsActive { get; set; }
}
