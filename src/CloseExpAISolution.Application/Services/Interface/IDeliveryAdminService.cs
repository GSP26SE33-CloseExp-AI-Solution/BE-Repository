using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IDeliveryAdminService
{
    Task<DeliveryGroupResponseDto> AssignGroupToStaffAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        Guid adminId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetPendingDeliveryGroupsAsync(
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetDraftDeliveryGroupsAsync(
        DraftDeliveryGroupQueryDto query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryGroupSummaryDto>> GenerateDraftGroupsAsync(
        GenerateDeliveryGroupDraftRequestDto request,
        Guid adminId,
        CancellationToken cancellationToken = default);

    Task<DeliveryGroupResponseDto> ConfirmDraftGroupAsync(
        Guid deliveryGroupId,
        Guid adminId,
        CancellationToken cancellationToken = default);
}