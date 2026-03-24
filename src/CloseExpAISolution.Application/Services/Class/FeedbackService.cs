using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class FeedbackService : IFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public FeedbackService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    #region Public Methods

    public async Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetAllFeedbacksAsync()
    {
        var feedbacks = await _unitOfWork.Repository<CustomerFeedback>().GetAllAsync();
        var userDict = await GetUserDictionary();

        var response = feedbacks.Select(f => MapFeedbackWithUser(f, userDict));
        return ApiResponse<IEnumerable<FeedbackResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<FeedbackResponseDto>> GetFeedbackByIdAsync(Guid id)
    {
        var feedback = await FindFeedbackById(id);
        if (feedback == null)
            return NotFound();

        var response = await MapFeedbackWithUserAsync(feedback);
        return ApiResponse<FeedbackResponseDto>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByUserIdAsync(Guid userId)
    {
        var feedbacks = await _unitOfWork.Repository<CustomerFeedback>()
            .FindAsync(f => f.UserId == userId);

        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == userId);
        var userName = user?.FullName ?? "Unknown";

        var response = feedbacks.Select(f =>
        {
            var dto = _mapper.Map<FeedbackResponseDto>(f);
            dto.UserName = userName;
            return dto;
        });

        return ApiResponse<IEnumerable<FeedbackResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<IEnumerable<FeedbackResponseDto>>> GetFeedbacksByOrderIdAsync(Guid orderId)
    {
        var feedbacks = await _unitOfWork.Repository<CustomerFeedback>()
            .FindAsync(f => f.OrderId == orderId);

        var userDict = await GetUserDictionary();
        var response = feedbacks.Select(f => MapFeedbackWithUser(f, userDict));

        return ApiResponse<IEnumerable<FeedbackResponseDto>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<FeedbackResponseDto>> CreateFeedbackAsync(Guid userId, CreateFeedbackRequestDto request)
    {
        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == request.OrderId);
        if (order == null)
            return Error("Đơn hàng không tồn tại");

        var existingFeedback = await _unitOfWork.Repository<CustomerFeedback>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.OrderId == request.OrderId);
        if (existingFeedback != null)
            return Error("Bạn đã đánh giá đơn hàng này rồi");

        var feedback = new CustomerFeedback
        {
            CustomerFeedbackId = Guid.NewGuid(),
            UserId = userId,
            OrderId = request.OrderId,
            Rating = (short)request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<CustomerFeedback>().AddAsync(feedback);
        await _unitOfWork.SaveChangesAsync();

        var response = await MapFeedbackWithUserAsync(feedback);
        return ApiResponse<FeedbackResponseDto>.SuccessResponse(response, "Đánh giá thành công");
    }

    public async Task<ApiResponse<FeedbackResponseDto>> UpdateFeedbackAsync(Guid feedbackId, Guid userId, UpdateFeedbackRequestDto request)
    {
        var feedback = await FindFeedbackById(feedbackId);
        if (feedback == null)
            return NotFound();

        if (feedback.UserId != userId)
            return Error("Bạn không có quyền sửa đánh giá này");

        if (request.Rating.HasValue)
            feedback.Rating = (short)request.Rating.Value;

        if (request.Comment != null)
            feedback.Comment = request.Comment;

        _unitOfWork.Repository<CustomerFeedback>().Update(feedback);
        await _unitOfWork.SaveChangesAsync();

        var response = await MapFeedbackWithUserAsync(feedback);
        return ApiResponse<FeedbackResponseDto>.SuccessResponse(response, "Cập nhật đánh giá thành công");
    }

    public async Task<ApiResponse<bool>> DeleteFeedbackAsync(Guid feedbackId, Guid userId, bool isAdmin)
    {
        var feedback = await FindFeedbackById(feedbackId);
        if (feedback == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy đánh giá");

        if (!isAdmin && feedback.UserId != userId)
            return ApiResponse<bool>.ErrorResponse("Bạn không có quyền xóa đánh giá này");

        _unitOfWork.Repository<CustomerFeedback>().Delete(feedback);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Xóa đánh giá thành công");
    }

    #endregion

    #region Private Helpers

    private async Task<CustomerFeedback?> FindFeedbackById(Guid id)
        => await _unitOfWork.Repository<CustomerFeedback>().FirstOrDefaultAsync(f => f.CustomerFeedbackId == id);

    private async Task<Dictionary<Guid, string>> GetUserDictionary()
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync();
        return users.ToDictionary(u => u.UserId, u => u.FullName);
    }

    private async Task<FeedbackResponseDto> MapFeedbackWithUserAsync(CustomerFeedback feedback)
    {
        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == feedback.UserId);
        var dto = _mapper.Map<FeedbackResponseDto>(feedback);
        dto.UserName = user?.FullName ?? "Unknown";
        return dto;
    }

    private FeedbackResponseDto MapFeedbackWithUser(CustomerFeedback feedback, Dictionary<Guid, string> userDict)
    {
        var dto = _mapper.Map<FeedbackResponseDto>(feedback);
        dto.UserName = userDict.GetValueOrDefault(feedback.UserId, "Unknown");
        return dto;
    }

    private static ApiResponse<FeedbackResponseDto> NotFound()
        => ApiResponse<FeedbackResponseDto>.ErrorResponse("Không tìm thấy đánh giá");

    private static ApiResponse<FeedbackResponseDto> Error(string message)
        => ApiResponse<FeedbackResponseDto>.ErrorResponse(message);

    #endregion
}
