using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class Notification
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Optional: when set, ties this row to an order (parent = placement; children = status / delivery updates).</summary>
    public Guid? OrderId { get; set; }

    /// <summary>Optional: root notification for this order thread (null = root row).</summary>
    public Guid? ParentNotificationId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.SystemAlert;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Order? Order { get; set; }
    public Notification? Parent { get; set; }
    public ICollection<Notification> Children { get; set; } = new List<Notification>();
}
