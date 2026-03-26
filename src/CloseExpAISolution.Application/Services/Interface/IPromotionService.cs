using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPromotionService
{
    Task<IEnumerable<AdminPromotionDto>> GetPromotionsAsync(CancellationToken cancellationToken = default);
    Task<AdminPromotionDto?> GetPromotionByIdAsync(Guid promotionId, CancellationToken cancellationToken = default);
    Task<AdminPromotionDto> CreatePromotionAsync(CreatePromotionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminPromotionDto?> UpdatePromotionAsync(Guid promotionId, UpdatePromotionRequestDto request, CancellationToken cancellationToken = default);
    Task<AdminPromotionDto?> UpdatePromotionStatusAsync(Guid promotionId, string status, CancellationToken cancellationToken = default);
    Task<PromotionValidationResultDto> ValidatePromotionAsync(Guid userId, ValidatePromotionRequestDto request, CancellationToken cancellationToken = default);
}
