using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class PromotionUsageService : IPromotionUsageService
{
    private readonly IUnitOfWork _unitOfWork;

    public PromotionUsageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PromotionUsageDto> RecordUsageAsync(Guid promotionId, Guid userId, Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Repository<PromotionUsage>()
            .FirstOrDefaultAsync(x => x.PromotionId == promotionId && x.UserId == userId && x.OrderId == orderId);
        if (existing != null)
            return await MapUsageAsync(existing, cancellationToken);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var usage = new PromotionUsage
            {
                UsageId = Guid.NewGuid(),
                PromotionId = promotionId,
                UserId = userId,
                OrderId = orderId,
                DiscountAmount = discountAmount,
                UsedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<PromotionUsage>().AddAsync(usage);

            var promotion = await _unitOfWork.Repository<Promotion>().FirstOrDefaultAsync(x => x.PromotionId == promotionId)
                ?? throw new InvalidOperationException("Promotion not found");
            promotion.UsedCount += 1;
            _unitOfWork.Repository<Promotion>().Update(promotion);

            await _unitOfWork.CommitTransactionAsync();
            return await MapUsageAsync(usage, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<PaginatedResult<PromotionUsageDto>> GetUsagesAsync(PromotionUsageFilterRequestDto request, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, request.PageNumber);
        var safeSize = Math.Clamp(request.PageSize, 1, 200);

        var usages = await _unitOfWork.Repository<PromotionUsage>().GetAllAsync();
        var filtered = usages.AsQueryable();
        if (request.FromUtc.HasValue) filtered = filtered.Where(x => x.UsedAt >= request.FromUtc.Value);
        if (request.ToUtc.HasValue) filtered = filtered.Where(x => x.UsedAt <= request.ToUtc.Value);
        if (request.UserId.HasValue) filtered = filtered.Where(x => x.UserId == request.UserId.Value);
        if (request.PromotionId.HasValue) filtered = filtered.Where(x => x.PromotionId == request.PromotionId.Value);

        var ordered = filtered.OrderByDescending(x => x.UsedAt).ToList();
        var pageItems = ordered.Skip((safePage - 1) * safeSize).Take(safeSize).ToList();
        var mapped = new List<PromotionUsageDto>(pageItems.Count);
        foreach (var item in pageItems)
        {
            mapped.Add(await MapUsageAsync(item, cancellationToken));
        }

        return new PaginatedResult<PromotionUsageDto>
        {
            Items = mapped,
            TotalResult = ordered.Count,
            Page = safePage,
            PageSize = safeSize
        };
    }

    private async Task<PromotionUsageDto> MapUsageAsync(PromotionUsage usage, CancellationToken cancellationToken)
    {
        var promotion = await _unitOfWork.Repository<Promotion>()
            .FirstOrDefaultAsync(x => x.PromotionId == usage.PromotionId);

        return new PromotionUsageDto
        {
            UsageId = usage.UsageId,
            PromotionId = usage.PromotionId,
            PromotionCode = promotion?.Code ?? string.Empty,
            UserId = usage.UserId,
            OrderId = usage.OrderId,
            DiscountAmount = usage.DiscountAmount,
            UsedAt = usage.UsedAt
        };
    }
}
