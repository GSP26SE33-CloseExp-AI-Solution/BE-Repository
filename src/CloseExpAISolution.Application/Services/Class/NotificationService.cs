using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetAllAsync()
    {
        var list = await _unitOfWork.Repository<Notification>().GetAllAsync();
        var ordered = list.OrderByDescending(n => n.CreatedAt).ToList();
        var userDict = await GetUserNameDictionary();
        var orderCodes = await GetOrderCodeDictionaryAsync(ordered.Where(n => n.OrderId.HasValue).Select(n => n.OrderId!.Value));
        var response = ordered.Select(n => MapWithUserName(n, userDict, orderCodes));
        return ApiResponse<IEnumerable<NotificationResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetByUserIdAsync(Guid userId)
    {
        var list = (await _unitOfWork.Repository<Notification>().FindAsync(n => n.UserId == userId))
            .OrderByDescending(n => n.CreatedAt)
            .ToList();
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == userId);
        var name = user?.FullName;
        var orderCodes = await GetOrderCodeDictionaryAsync(list.Where(n => n.OrderId.HasValue).Select(n => n.OrderId!.Value));

        var response = list.Select(n =>
        {
            var dto = _mapper.Map<NotificationResponseDto>(n);
            dto.UserFullName = name;
            if (n.OrderId.HasValue)
                dto.OrderCode = orderCodes.GetValueOrDefault(n.OrderId.Value);
            return dto;
        });

        return ApiResponse<IEnumerable<NotificationResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<NotificationResponseDto>>> GetMyOrderNotificationsAsync(Guid userId, Guid orderId)
    {
        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null || order.UserId != userId)
            return ApiResponse<IEnumerable<NotificationResponseDto>>.ErrorResponse(
                "Không tìm thấy đơn hàng hoặc bạn không có quyền xem.");

        var list = await _unitOfWork.Repository<Notification>()
            .FindAsync(n => n.UserId == userId && n.OrderId == orderId);
        var ordered = list.OrderBy(n => n.CreatedAt).ToList();
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == userId);

        var response = ordered.Select(n =>
        {
            var dto = _mapper.Map<NotificationResponseDto>(n);
            dto.UserFullName = user?.FullName;
            dto.OrderCode = order.OrderCode;
            return dto;
        });

        return ApiResponse<IEnumerable<NotificationResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<NotificationResponseDto>> GetByIdAsync(Guid notificationId, Guid requesterId, bool isAdmin)
    {
        var notification = await FindById(notificationId);
        if (notification == null)
            return NotFound();

        if (!isAdmin && notification.UserId != requesterId)
            return Error("Bạn không có quyền xem thông báo này");

        var dto = await MapWithUserNameAsync(notification);
        return ApiResponse<NotificationResponseDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<NotificationResponseDto>> CreateAsync(CreateNotificationRequestDto request)
    {
        var userExists = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId);
        if (userExists == null)
            return Error("Người dùng không tồn tại");

        var entity = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Type = request.Type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Notification>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapWithUserNameAsync(entity);
        return ApiResponse<NotificationResponseDto>.SuccessResponse(dto, "Tạo thông báo thành công");
    }

    public async Task<ApiResponse<NotificationResponseDto>> UpdateAsync(
        Guid notificationId,
        Guid requesterId,
        bool isAdmin,
        UpdateNotificationRequestDto request)
    {
        var notification = await FindById(notificationId);
        if (notification == null)
            return NotFound();

        if (!isAdmin)
        {
            if (notification.UserId != requesterId)
                return Error("Bạn không có quyền sửa thông báo này");

            var triesContentEdit = request.Title != null || request.Content != null || request.Type.HasValue;
            if (triesContentEdit)
                return Error("Bạn chỉ có thể đánh dấu đã đọc");

            if (request.IsRead.HasValue)
                notification.IsRead = request.IsRead.Value;
        }
        else
        {
            if (request.Title != null)
                notification.Title = request.Title.Trim();
            if (request.Content != null)
                notification.Content = request.Content.Trim();
            if (request.Type.HasValue)
                notification.Type = request.Type.Value;
            if (request.IsRead.HasValue)
                notification.IsRead = request.IsRead.Value;
        }

        _unitOfWork.Repository<Notification>().Update(notification);
        await _unitOfWork.SaveChangesAsync();

        var dto = await MapWithUserNameAsync(notification);
        return ApiResponse<NotificationResponseDto>.SuccessResponse(dto, "Cập nhật thông báo thành công");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid notificationId, Guid requesterId, bool isAdmin)
    {
        var notification = await FindById(notificationId);
        if (notification == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy thông báo");

        if (!isAdmin && notification.UserId != requesterId)
            return ApiResponse<bool>.ErrorResponse("Bạn không có quyền xóa thông báo này");

        _unitOfWork.Repository<Notification>().Delete(notification);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Xóa thông báo thành công");
    }

    private async Task<Notification?> FindById(Guid id)
        => await _unitOfWork.Repository<Notification>().FirstOrDefaultAsync(n => n.NotificationId == id);

    private async Task<Dictionary<Guid, string>> GetUserNameDictionary()
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync();
        return users.ToDictionary(u => u.UserId, u => u.FullName);
    }

    private async Task<Dictionary<Guid, string>> GetOrderCodeDictionaryAsync(IEnumerable<Guid> orderIds)
    {
        var ids = orderIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        var orders = await _unitOfWork.Repository<Order>().FindAsync(o => ids.Contains(o.OrderId));
        return orders.ToDictionary(o => o.OrderId, o => o.OrderCode);
    }

    private NotificationResponseDto MapWithUserName(
        Notification n,
        Dictionary<Guid, string> userDict,
        Dictionary<Guid, string> orderCodes)
    {
        var dto = _mapper.Map<NotificationResponseDto>(n);
        dto.UserFullName = userDict.GetValueOrDefault(n.UserId);
        if (n.OrderId.HasValue)
            dto.OrderCode = orderCodes.GetValueOrDefault(n.OrderId.Value);
        return dto;
    }

    private async Task<NotificationResponseDto> MapWithUserNameAsync(Notification n)
    {
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == n.UserId);
        var dto = _mapper.Map<NotificationResponseDto>(n);
        dto.UserFullName = user?.FullName;
        if (n.OrderId.HasValue)
        {
            var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == n.OrderId.Value);
            dto.OrderCode = order?.OrderCode;
        }

        return dto;
    }

    private static ApiResponse<NotificationResponseDto> NotFound()
        => ApiResponse<NotificationResponseDto>.ErrorResponse("Không tìm thấy thông báo");

    private static ApiResponse<NotificationResponseDto> Error(string message)
        => ApiResponse<NotificationResponseDto>.ErrorResponse(message);
}
