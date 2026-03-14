using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs.Request;

public class ConfirmPackagingOrderRequestDto
{
    public string? Notes { get; set; }
}

public class CollectPackagingOrderRequestDto
{
    [Required(ErrorMessage = "Ghi chú thu gom là bắt buộc")]
    public string Notes { get; set; } = string.Empty;
}

public class CompletePackagingOrderRequestDto
{
    public string? Notes { get; set; }
}
