using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IFeedbackService
{
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetAllFeedbacksAsync();
    Task<ApiResponse<FeedbackResponseDto>> GetFeedbackByIdAsync(Guid id);
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByUserIdAsync(Guid userId);
    Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByOrderIdAsync(Guid orderId);
    Task<ApiResponse<FeedbackResponseDto>> CreateFeedbackAsync(Guid userId, CreateFeedbackRequestDto request);
    Task<ApiResponse<FeedbackResponseDto>> UpdateFeedbackAsync(Guid feedbackId, Guid userId, UpdateFeedbackRequestDto request);
    Task<ApiResponse<bool>> DeleteFeedbackAsync(Guid feedbackId, Guid userId, bool isAdmin);
}