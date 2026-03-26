using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class ValidatePromotionRequestDto
{
    public Guid? PromotionId { get; set; }

    [MaxLength(50)]
    public string? PromotionCode { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal TotalAmount { get; set; }
}

public class ApplyPromotionToOrderRequestDto
{
    public Guid? PromotionId { get; set; }

    [MaxLength(50)]
    public string? PromotionCode { get; set; }
}

public class PromotionUsageFilterRequestDto
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PromotionId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
