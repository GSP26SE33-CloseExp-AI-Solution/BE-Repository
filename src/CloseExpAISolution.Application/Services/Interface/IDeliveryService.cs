using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IDeliveryService
{
    Task<IEnumerable<DeliveryGroupSummaryDto>> GetAvailableDeliveryGroupsAsync(
        DateTime? deliveryDate = null,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<DeliveryGroupSummaryDto> Items, int TotalCount)> GetMyDeliveryGroupsAsync(
        Guid deliveryStaffId,
        string? status = null,
        DateTime? deliveryDate = null,
        int pageNumber = 1,
        int pageSize = 20,
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

    Task<DeliveryOrderResponseDto> ReportDeliveryFailureAsync(
        Guid orderId,
        Guid deliveryStaffId,
        ReportDeliveryFailureRequestDto request,
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
}