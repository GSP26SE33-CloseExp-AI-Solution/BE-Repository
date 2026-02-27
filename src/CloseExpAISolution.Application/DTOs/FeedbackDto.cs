using System.ComponentModel.DataAnnotations;

namespace CloseExpAISolution.Application.DTOs;

public class FeedbackResponseDto
{
    public Guid FeedbackId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateFeedbackRequestDto
{
    [Required(ErrorMessage = "Mã đơn hàng không được để trống")]
    public Guid OrderId { get; set; }

    [Required(ErrorMessage = "Đánh giá không được để trống")]
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự")]
    public string? Comment { get; set; }
}

public class UpdateFeedbackRequestDto
{
    [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
    public int? Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Bình luận không được vượt quá 1000 ký tự")]
    public string? Comment { get; set; }
}
