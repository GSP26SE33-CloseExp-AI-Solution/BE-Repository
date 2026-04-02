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

    /// <summary>
    /// Admin lịch / lọc: <paramref name="status"/> null hoặc rỗng = mọi nhóm trừ Draft;
    /// Pending = chờ shipper Accept (có DeliveryStaffId); các enum khác lọc đúng tên trạng thái.
    /// </summary>
    Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetDeliveryGroupsForAdminAsync(
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? status = null,
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

    Task<MoveOrderToDraftGroupResultDto> MoveOrderToDraftGroupAsync(
        Guid orderId,
        MoveOrderToDraftGroupRequestDto request,
        Guid adminId,
        CancellationToken cancellationToken = default);
}