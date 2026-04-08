using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface INotificationService
{
    Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetAllAsync();

    Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetByUserIdAsync(Guid userId);

    /// <summary>Timeline for one order (placement + status / delivery updates), newest last.</summary>
    Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetMyOrderNotificationsAsync(Guid userId, Guid orderId);

    Task<ApiResponse<NotificationResponseDto>> GetByIdAsync(Guid notificationId, Guid requesterId, bool isAdmin);

    Task<ApiResponse<NotificationResponseDto>> CreateAsync(CreateNotificationRequestDto request);

    Task<ApiResponse<NotificationResponseDto>> UpdateAsync(
        Guid notificationId,
        Guid requesterId,
        bool isAdmin,
        UpdateNotificationRequestDto request);

    Task<ApiResponse<bool>> DeleteAsync(Guid notificationId, Guid requesterId, bool isAdmin);
}
