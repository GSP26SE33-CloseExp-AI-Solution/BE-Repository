namespace CloseExpAISolution.Application.DTOs.Response;

public class UserImageResponseDto
{
    public Guid ImageId { get; set; }
    public Guid UserId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? PreSignedUrl { get; set; }
    public string ImageType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime UploadedAt { get; set; }
}
