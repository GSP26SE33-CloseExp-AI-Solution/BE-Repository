using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPromotionAnalyticsService
{
    Task<PromotionAnalyticsOverviewDto> GetOverviewAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminPromotionDto>> GetTopPromotionsAsync(string metric, int limit, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<IEnumerable<PromotionTrendPointDto>> GetPromotionTrendAsync(Guid promotionId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}
