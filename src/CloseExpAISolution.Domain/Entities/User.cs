using System.ComponentModel.DataAnnotations.Schema;

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
    public DateTime UpdatedAt { get; set; }
    [NotMapped]
    public DateTime UpdateAt { get => UpdatedAt; set => UpdatedAt = value; }

    // Email verification (OTP)
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public int OtpFailedCount { get; set; }
    public DateTime? EmailVerifiedAt { get; set; }

    // Google OAuth
    public string? GoogleId { get; set; }

    public ICollection<UserImage> UserImages { get; set; } = new List<UserImage>();
    public ICollection<CustomerFeedback> Feedbacks { get; set; } = new List<CustomerFeedback>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<DeliveryLog> DeliveryLogs { get; set; } = new List<DeliveryLog>();
    [NotMapped]
    public ICollection<DeliveryLog> DeliveryRecords { get => DeliveryLogs; set => DeliveryLogs = value; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

