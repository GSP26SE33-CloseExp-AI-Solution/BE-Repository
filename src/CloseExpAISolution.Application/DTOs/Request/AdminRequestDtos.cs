using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class UpsertTimeSlotRequestDto
{
    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }
}

public class UpsertCollectionPointRequestDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string AddressLine { get; set; } = string.Empty;

    [Required]
    [Range(-90.0, 90.0)]
    public decimal Latitude { get; set; }

    [Required]
    [Range(-180.0, 180.0)]
    public decimal Longitude { get; set; }
}

public class UpsertSystemConfigRequestDto
{
    [Required]
    [MaxLength(1000)]
    public string ConfigValue { get; set; } = string.Empty;
}

public class UpsertUnitRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Symbol { get; set; } = string.Empty;
}

public class CreatePromotionRequestDto
{
    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DiscountType { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal DiscountValue { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Draft";
}

public class UpdatePromotionRequestDto
{
    public Guid? CategoryId { get; set; }

    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(50)]
    public string? DiscountType { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? DiscountValue { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }
}

public class UpdatePromotionStatusRequestDto
{
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;
}
