using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IAdminService
{
    Task<AdminDashboardOverviewDto> GetDashboardOverviewAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminRevenueTrendPointDto>> GetRevenueTrendAsync(int days, CancellationToken cancellationToken = default);
    Task<IEnumerable<AdminSlaAlertDto>> GetSlaAlertsAsync(int thresholdMinutes, int top, CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminTimeSlotDto>> GetTimeSlotsAsync(CancellationToken cancellationToken = default);
    Task<AdminTimeSlotDto> CreateTimeSlotAsync(UpsertTimeSlotRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminTimeSlotDto?> UpdateTimeSlotAsync(Guid timeSlotId, UpsertTimeSlotRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTimeSlotAsync(Guid timeSlotId, CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminCollectionPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default);
    Task<AdminCollectionPointDto> CreateCollectionPointAsync(UpsertCollectionPointRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminCollectionPointDto?> UpdateCollectionPointAsync(Guid collectionId, UpsertCollectionPointRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCollectionPointAsync(Guid collectionId, CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminSystemConfigDto>> GetSystemConfigsAsync(CancellationToken cancellationToken = default);
    Task<AdminSystemConfigDto> UpsertSystemConfigAsync(string configKey, UpsertSystemConfigRequestDto request, CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminUnitDto>> GetUnitsAsync(CancellationToken cancellationToken = default);
    Task<AdminUnitDto> CreateUnitAsync(UpsertUnitRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminUnitDto?> UpdateUnitAsync(Guid unitId, UpsertUnitRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUnitAsync(Guid unitId, CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminPromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default);
    Task<AdminPromotionDto> CreatePromotionAsync(CreatePromotionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminPromotionDto?> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminPromotionDto?> UpdatePromotionStatusAsync(Guid promotionId, string status, CancellationToken cancellationToken = default);

    Task<PaginatedResult<AdminAiPriceHistoryDto>> GetAiPriceHistoriesAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
