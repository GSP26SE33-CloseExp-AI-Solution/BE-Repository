using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IFeedbackService
{
    /// <summary>Get all feedbacks (Admin)</summary>
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetAllFeedbacksAsync();

    /// <summary>Get feedback by ID</summary>
    Task<ApiResponse<FeedbackResponseDto>> GetFeedbackByIdAsync(Guid id);

    /// <summary>Get all feedbacks by user ID</summary>
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByUserIdAsync(Guid userId);

    /// <summary>Get all feedbacks for an order</summary>
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByOrderIdAsync(Guid orderId);

    /// <summary>Create new feedback (User)</summary>
    Task<ApiResponse<FeedbackResponseDto>> CreateFeedbackAsync(Guid userId, CreateFeedbackRequestDto request);

    /// <summary>Update feedback (Owner only)</summary>
    Task<ApiResponse<FeedbackResponseDto>> UpdateFeedbackAsync(Guid feedbackId, Guid userId, UpdateFeedbackRequestDto request);

    /// <summary>Delete feedback (Owner or Admin)</summary>
    Task<ApiResponse<bool>> DeleteFeedbackAsync(Guid feedbackId, Guid userId, bool isAdmin);
}
