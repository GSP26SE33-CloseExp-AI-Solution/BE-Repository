using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class PromotionService : IPromotionService
{
    private readonly IUnitOfWork _unitOfWork;

    public PromotionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AdminPromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default)
    {
        var promotions = await _unitOfWork.Repository<Promotion>().GetAllAsync();
        return promotions.OrderByDescending(x => x.StartDate).Select(MapPromotion).ToList();
    }

    public async Task<AdminPromotionDto?> GetPromotionByIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == promotionId);
        return entity == null ? null : MapPromotion(entity);
    }

    public async Task<AdminPromotionDto> CreatePromotionAsync(CreatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate < request.StartDate)
            throw new InvalidOperationException("EndDate phải lớn hơn hoặc bằng StartDate");

        if (await _unitOfWork.Repository<Promotion>().ExistsAsync(x => x.Code == request.Code.Trim()))
            throw new InvalidOperationException("Mã khuyến mãi đã tồn tại");

        var entity = new Promotion
        {
            PromotionId = Guid.NewGuid(),
            Code = request.Code.Trim(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            DiscountType = request.DiscountType.Trim(),
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxUsage = request.MaxUsage,
            PerUserLimit = request.PerUserLimit,
            UsedCount = 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = Enum.Parse<PromotionState>(request.Status.Trim(), true)
        };

        await _unitOfWork.Repository<Promotion>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPromotion(entity);
    }

    public async Task<AdminPromotionDto?> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == promotionId);
        if (entity == null)
            return null;

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var normalizedCode = request.Code.Trim();
            var duplicatedCode = await _unitOfWork.Repository<Promotion>()
                .ExistsAsync(x => x.PromotionId != promotionId && x.Code == normalizedCode);
            if (duplicatedCode)
                throw new InvalidOperationException("Mã khuyến mãi đã tồn tại");

            entity.Code = normalizedCode;
        }

        if (request.CategoryId.HasValue) entity.CategoryId = request.CategoryId.Value;
        if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.DiscountType)) entity.DiscountType = request.DiscountType.Trim();
        if (request.DiscountValue.HasValue) entity.DiscountValue = request.DiscountValue.Value;
        if (request.MinOrderAmount.HasValue) entity.MinOrderAmount = request.MinOrderAmount.Value;
        if (request.MaxDiscountAmount.HasValue) entity.MaxDiscountAmount = request.MaxDiscountAmount.Value;
        if (request.MaxUsage.HasValue) entity.MaxUsage = request.MaxUsage.Value;
        if (request.PerUserLimit.HasValue) entity.PerUserLimit = request.PerUserLimit.Value;
        if (request.StartDate.HasValue) entity.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) entity.EndDate = request.EndDate.Value;
        if (entity.EndDate < entity.StartDate)
            throw new InvalidOperationException("EndDate phải lớn hơn hoặc bằng StartDate");
        if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = Enum.Parse<PromotionState>(request.Status.Trim(), true);

        _unitOfWork.Repository<Promotion>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapPromotion(entity);
    }

    public async Task<AdminPromotionDto?> UpdatePromotionStatusAsync(Guid promotionId, string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required", nameof(status));

        var entity = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == promotionId);
        if (entity == null)
            return null;

        entity.Status = Enum.Parse<PromotionState>(status.Trim(), true);
        _unitOfWork.Repository<Promotion>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPromotion(entity);
    }

    public async Task<PromotionValidationResultDto> ValidatePromotionAsync(Guid userId, ValidatePromotionRequestDto request, CancellationToken cancellationToken = default)
    {
        Promotion? promotion = null;
        if (request.PromotionId.HasValue)
        {
            promotion = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == request.PromotionId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            var code = request.PromotionCode.Trim();
            promotion = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.Code == code);
        }

        if (promotion == null)
            return Invalid("Không tìm thấy khuyến mãi", request.TotalAmount);

        if (promotion.Status != PromotionState.Active)
            return Invalid("Khuyến mãi chưa khả dụng", request.TotalAmount, promotion);

        var now = DateTime.UtcNow;
        if (now < promotion.StartDate || now > promotion.EndDate)
            return Invalid("Khuyến mãi ngoài thời gian hiệu lực", request.TotalAmount, promotion);

        if (promotion.UsedCount >= promotion.MaxUsage)
            return Invalid("Khuyến mãi đã hết lượt sử dụng", request.TotalAmount, promotion);

        if (promotion.MinOrderAmount.HasValue && request.TotalAmount < promotion.MinOrderAmount.Value)
            return Invalid($"Đơn hàng chưa đạt giá trị tối thiểu {promotion.MinOrderAmount.Value}", request.TotalAmount, promotion);

        var userUsageCount = await _unitOfWork.Repository<PromotionUsage>()
            .CountAsync(x => x.PromotionId == promotion.PromotionId && x.UserId == userId);
        if (userUsageCount >= promotion.PerUserLimit)
            return Invalid("Bạn đã đạt giới hạn sử dụng khuyến mãi này", request.TotalAmount, promotion);

        var discountAmount = CalculateDiscount(promotion, request.TotalAmount);
        return new PromotionValidationResultDto
        {
            IsValid = true,
            Message = "Khuyến mãi hợp lệ",
            PromotionId = promotion.PromotionId,
            PromotionCode = promotion.Code,
            OriginalAmount = request.TotalAmount,
            DiscountAmount = discountAmount,
            FinalAmount = Math.Max(0, request.TotalAmount - discountAmount)
        };
    }

    private static decimal CalculateDiscount(Promotion promotion, decimal totalAmount)
    {
        decimal discount = promotion.DiscountType.Equals("Percent", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(totalAmount * (promotion.DiscountValue / 100m), 2)
            : Math.Min(totalAmount, promotion.DiscountValue);

        if (promotion.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);

        return Math.Max(0, discount);
    }

    private static PromotionValidationResultDto Invalid(string message, decimal originalAmount, Promotion? promotion = null) => new()
    {
        IsValid = false,
        Message = message,
        PromotionId = promotion?.PromotionId,
        PromotionCode = promotion?.Code,
        OriginalAmount = originalAmount,
        DiscountAmount = 0,
        FinalAmount = originalAmount
    };

    public static AdminPromotionDto MapPromotion(Promotion x) => new()
    {
        PromotionId = x.PromotionId,
        Code = x.Code,
        CategoryId = x.CategoryId,
        Name = x.Name,
        DiscountType = x.DiscountType,
        DiscountValue = x.DiscountValue,
        MinOrderAmount = x.MinOrderAmount,
        MaxDiscountAmount = x.MaxDiscountAmount,
        MaxUsage = x.MaxUsage,
        UsedCount = x.UsedCount,
        PerUserLimit = x.PerUserLimit,
        StartDate = x.StartDate,
        EndDate = x.EndDate,
        Status = x.Status.ToString()
    };
}
