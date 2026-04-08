using System.ComponentModel.DataAnnotations;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.DTOs;

public class NotificationResponseDto
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string? UserFullName { get; set; }

    /// <summary>When set, this notification belongs to an order thread (placement = root, others = children).</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Root notification id for this thread; null for the placement row.</summary>
    public Guid? ParentNotificationId { get; set; }

    /// <summary>Populated when <see cref="OrderId"/> is set.</summary>
    public string? OrderCode { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequestDto
{
    [Required(ErrorMessage = "Người nhận không được để trống")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(500, ErrorMessage = "Tiêu đề không được vượt quá 500 ký tự")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [StringLength(4000, ErrorMessage = "Nội dung không được vượt quá 4000 ký tự")]
    public string Content { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.SystemAlert;
}

public class UpdateNotificationRequestDto
{
    [StringLength(500)]
    public string? Title { get; set; }

    [StringLength(4000)]
    public string? Content { get; set; }

    public NotificationType? Type { get; set; }

    public bool? IsRead { get; set; }
}
