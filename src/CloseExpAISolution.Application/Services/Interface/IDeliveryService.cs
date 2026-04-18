using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IDeliveryService
{
    /// <summary>Nhóm đang chờ shipper xác nhận nhận (Pending + đã gán đúng staff).</summary>
    Task<IEnumerable<DeliveryGroupSummaryDto>> GetAvailableDeliveryGroupsAsync(
        Guid deliveryStaffId,
        DateTime? deliveryDate = null,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetMyDeliveryGroupsAsync(
        Guid deliveryStaffId,
        string? status = null,
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        double? currentLatitude = null,
        double? currentLongitude = null,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<DeliveryGroupSummaryDto>> GetMyDeliveryWorkQueueAsync(
        Guid deliveryStaffId,
        string? status = null,
        DateTime? deliveryDate = null,
        int limit = 10,
        string? sortBy = null,
        double? currentLatitude = null,
        double? currentLongitude = null,
        CancellationToken cancellationToken = default);

    Task<DeliveryGroupResponseDto?> GetDeliveryGroupDetailAsync(
        Guid deliveryGroupId,
        CancellationToken cancellationToken = default);

    Task<DeliveryGroupResponseDto> AcceptDeliveryGroupAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        AcceptDeliveryGroupRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DeliveryGroupResponseDto> StartDeliveryAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        StartDeliveryRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DeliveryOrderResponseDto?> GetOrderDetailForDeliveryAsync(
        Guid orderId,
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default);

    Task<DeliveryOrderResponseDto> ConfirmDeliveryAsync(
        Guid orderId,
        Guid deliveryStaffId,
        ConfirmDeliveryRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Shipper upload ảnh chứng minh lên R2; trả về URL để gửi vào <see cref="ConfirmDeliveryRequestDto.ProofImageUrl"/>.</summary>
    Task<DeliveryProofUploadResponseDto> UploadDeliveryProofImageAsync(
        Guid orderId,
        Guid deliveryStaffId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<DeliveryOrderResponseDto> ReportDeliveryFailureAsync(
        Guid orderId,
        Guid deliveryStaffId,
        ReportDeliveryFailureRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DeliveryOrderResponseDto> ConfirmOrderReceiptByCustomerAsync(
        Guid orderId,
        Guid customerId,
        ConfirmOrderReceiptRequestDto request,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<DeliveryRecordResponseDto> Items, int TotalCount)> GetDeliveryHistoryAsync(
        Guid deliveryStaffId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<DeliveryStatsResponseDto> GetDeliveryStatsAsync(
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default);

    Task<DeliveryGroupResponseDto> CompleteDeliveryGroupAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimize stop order for the group (Mapbox Matrix + heuristic) and return driving polyline (Directions).
    /// </summary>
    Task<DeliveryRoutePlanResponseDto> ComputeDeliveryRoutePlanAsync(
        Guid deliveryGroupId,
        Guid deliveryStaffId,
        DeliveryRoutePlanRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-confirm lines in DeliveredWaitConfirm after configured waiting days,
    /// then recompute order status from all lines.
    /// </summary>
    Task<int> AutoConfirmDeliveredOrdersAsync(CancellationToken cancellationToken = default);
}