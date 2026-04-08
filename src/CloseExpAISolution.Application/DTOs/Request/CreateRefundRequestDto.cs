using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class CreateRefundRequestDto
{
    [Required]
    public Guid OrderId { get; set; }

    [Required]
    public Guid TransactionId { get; set; }

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
}
