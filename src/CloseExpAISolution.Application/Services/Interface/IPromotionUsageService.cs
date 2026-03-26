using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPromotionUsageService
{
    Task<PromotionUsageDto> RecordUsageAsync(Guid promotionId, Guid userId, Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default);
    Task<PaginatedResult<PromotionUsageDto>> GetUsagesAsync(PromotionUsageFilterRequestDto request, CancellationToken cancellationToken = default);
}
