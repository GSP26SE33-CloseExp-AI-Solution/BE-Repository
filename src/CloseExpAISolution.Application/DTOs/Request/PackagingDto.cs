using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class ConfirmPackagingOrderRequestDto
{
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
    public string? Notes { get; set; }
}

public class CollectPackagingOrderRequestDto
{
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
    [Required(ErrorMessage = "Ghi chú thu gom là bắt buộc")]
    public string Notes { get; set; } = string.Empty;
}

public class CompletePackagingOrderRequestDto
{
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
    public string? Notes { get; set; }
}

public class FailPackagingOrderRequestDto
{
    public IReadOnlyList<Guid>? OrderItemIds { get; set; }
    [Required(ErrorMessage = "Lý do thất bại là bắt buộc")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Lý do thất bại không được để trống")]
    [MaxLength(2000)]
    public string FailureReason { get; set; } = string.Empty;
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
