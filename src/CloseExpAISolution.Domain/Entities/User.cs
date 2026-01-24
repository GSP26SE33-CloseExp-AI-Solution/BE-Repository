namespace CloseExpAISolution.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }
    public Role? Role { get; set; }

    public string Status { get; set; } = string.Empty;
    public int FailedLoginCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public ICollection<UserImage> UserImages { get; set; } = new List<UserImage>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<DeliveryRecord> DeliveryRecords { get; set; } = new List<DeliveryRecord>();
}

