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
    private readonly IRealtimeNotificationPublisher? _realtimePublisher;

    public PromotionService(IUnitOfWork unitOfWork, IRealtimeNotificationPublisher? realtimePublisher = null)
    {
        _unitOfWork = unitOfWork;
        _realtimePublisher = realtimePublisher;
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

        if (!TryParsePromotionStatus(request.Status, out var parsedStatus))
            throw new InvalidOperationException("Status không hợp lệ. Giá trị cho phép: Draft, Active.");

        if (parsedStatus is not (PromotionState.Draft or PromotionState.Active))
            throw new InvalidOperationException("Khi tạo mới chỉ được chọn Draft hoặc Active.");

        await EnsureCategoryExistsAsync(request.CategoryId);

        var entity = new Promotion
        {
            PromotionId = Guid.NewGuid(),
            Code = request.Code.Trim(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            DiscountType = NormalizeDiscountType(request.DiscountType),
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MaxUsage = request.MaxUsage,
            PerUserLimit = request.PerUserLimit,
            UsedCount = 0,
            StartDate = ToUtcDateTime(request.StartDate),
            EndDate = ToUtcDateTime(request.EndDate),
            Status = parsedStatus
        };

        await _unitOfWork.Repository<Promotion>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (entity.Status == PromotionState.Active)
            await NotifyVendorsAboutNewPromotionAsync(entity, cancellationToken);

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

        if (request.CategoryId.HasValue)
        {
            await EnsureCategoryExistsAsync(request.CategoryId.Value);
            entity.CategoryId = request.CategoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            entity.Name = request.Name.Trim();

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

        if (!TryParsePromotionStatus(status, out var parsedStatus))
            throw new ArgumentException("Status không hợp lệ. Giá trị cho phép: Draft, Active, Inactive, Expired.", nameof(status));

        var wasActive = entity.Status == PromotionState.Active;
        entity.Status = parsedStatus;
        _unitOfWork.Repository<Promotion>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!wasActive && parsedStatus == PromotionState.Active)
            await NotifyVendorsAboutNewPromotionAsync(entity, cancellationToken);

        return MapPromotion(entity);
    }

    public async Task<bool> DeletePromotionAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == promotionId);
        if (entity == null)
            return false;

        if (entity.UsedCount > 0)
            throw new InvalidOperationException("Không thể xóa khuyến mãi đã có lượt sử dụng.");

        var hasUsages = await _unitOfWork.Repository<PromotionUsage>()
            .ExistsAsync(x => x.PromotionId == promotionId);
        if (hasUsages)
            throw new InvalidOperationException("Không thể xóa khuyến mãi đã có lượt sử dụng.");

        var hasOrders = await _unitOfWork.Repository<Order>()
            .ExistsAsync(x => x.PromotionId == promotionId);
        if (hasOrders)
            throw new InvalidOperationException("Không thể xóa khuyến mãi đã được gắn vào đơn hàng.");

        _unitOfWork.Repository<Promotion>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureCategoryExistsAsync(Guid categoryId)
    {
        var exists = await _unitOfWork.Repository<Category>()
            .ExistsAsync(c => c.CategoryId == categoryId);
        if (!exists)
            throw new InvalidOperationException("Danh mục áp dụng không tồn tại.");
    }

    private static DateTime ToUtcDateTime(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private static string NormalizeDiscountType(string discountType)
    {
        if (string.IsNullOrWhiteSpace(discountType))
            return "Percent";

        if (discountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase))
            return "Percent";

        return discountType.Trim();
    }

    private static bool TryParsePromotionStatus(string status, out PromotionState parsedStatus)
        => Enum.TryParse(status?.Trim(), ignoreCase: true, out parsedStatus);

    public async Task<IReadOnlyList<CustomerPromotionOptionDto>> GetAvailableForCustomerAsync(
        Guid userId,
        decimal cartSubtotal,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var promotions = (await _unitOfWork.Repository<Promotion>()
                .FindAsync(p => p.Status == PromotionState.Active))
            .Where(p => now >= p.StartDate && now <= p.EndDate)
            .OrderByDescending(p => p.StartDate)
            .ToList();

        if (promotions.Count == 0)
            return Array.Empty<CustomerPromotionOptionDto>();

        var promotionIds = promotions.Select(p => p.PromotionId).ToList();
        var usages = (await _unitOfWork.Repository<PromotionUsage>()
                .FindAsync(u => u.UserId == userId && promotionIds.Contains(u.PromotionId)))
            .GroupBy(u => u.PromotionId)
            .ToDictionary(g => g.Key, g => g.Count());

        var options = new List<CustomerPromotionOptionDto>();
        foreach (var promotion in promotions)
        {
            usages.TryGetValue(promotion.PromotionId, out var userUsageCount);
            var exhaustedGlobal = promotion.UsedCount >= promotion.MaxUsage;
            var exhaustedUser = userUsageCount >= promotion.PerUserLimit;
            var belowMin = promotion.MinOrderAmount.HasValue && cartSubtotal < promotion.MinOrderAmount.Value;

            string? disabledReason = null;
            var isDisabled = false;
            if (exhaustedUser)
            {
                isDisabled = true;
                disabledReason = "Bạn đã sử dụng mã này";
            }
            else if (exhaustedGlobal)
            {
                isDisabled = true;
                disabledReason = "Mã đã hết lượt";
            }
            else if (belowMin)
            {
                isDisabled = true;
                disabledReason = $"Đơn tối thiểu {promotion.MinOrderAmount!.Value:N0}đ";
            }

            var previewDiscount = isDisabled ? 0m : CalculateDiscount(promotion, cartSubtotal);
            options.Add(new CustomerPromotionOptionDto
            {
                PromotionId = promotion.PromotionId,
                Code = promotion.Code,
                Name = promotion.Name,
                DiscountType = promotion.DiscountType,
                DiscountValue = promotion.DiscountValue,
                MinOrderAmount = promotion.MinOrderAmount,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                PerUserLimit = promotion.PerUserLimit,
                UserUsageCount = userUsageCount,
                CanApply = !isDisabled,
                IsDisabled = isDisabled,
                DisabledReason = disabledReason,
                PreviewDiscountAmount = previewDiscount,
                PreviewFinalAmount = Math.Max(0, cartSubtotal - previewDiscount)
            });
        }

        return options;
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
            || promotion.DiscountType.Equals("Percentage", StringComparison.OrdinalIgnoreCase)
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

    private async Task NotifyVendorsAboutNewPromotionAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        var vendors = (await _unitOfWork.Repository<User>()
                .FindAsync(u => u.RoleId == (int)RoleUser.Vendor && u.Status == UserState.Active))
            .ToList();

        if (vendors.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var title = "Khuyến mãi mới";
        var content =
            $"Mã {promotion.Code} — {promotion.Name}. Áp dụng tại bước thanh toán trước {promotion.EndDate:dd/MM/yyyy}.";

        var notifications = vendors.Select(v => new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = v.UserId,
            Title = title,
            Content = content,
            Type = NotificationType.Promotion,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        await _unitOfWork.Repository<Notification>().AddRangeAsync(notifications);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_realtimePublisher != null)
            await _realtimePublisher.PublishManyAsync(notifications, cancellationToken);
    }

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
