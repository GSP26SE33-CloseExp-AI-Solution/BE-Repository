using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class CreateCategoryRequestDto
{
    public Guid? ParentCatId { get; set; }

    public bool IsFreshFood { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CatIconUrl { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequestDto
{
    public Guid? ParentCatId { get; set; }

    public bool IsFreshFood { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CatIconUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
