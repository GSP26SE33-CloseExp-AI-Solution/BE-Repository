using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Domain.Entities;

public class UserImage
{
    public Guid ImageId { get; set; }
    public Guid UserId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public ImageValidationState Status { get; set; } = ImageValidationState.Pending;
    public DateTime UploadedAt { get; set; }

    public User? User { get; set; }
}