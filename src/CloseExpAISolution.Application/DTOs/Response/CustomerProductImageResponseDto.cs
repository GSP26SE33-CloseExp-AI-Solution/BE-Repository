namespace CloseExpAISolution.Application.DTOs.Response;

public class CustomerProductImageResponseDto
{
    public Guid ProductImageId { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? PreSignedUrl { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
}
