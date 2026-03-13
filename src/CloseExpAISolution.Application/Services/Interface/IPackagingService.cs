using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IPackagingService
{
    Task<(IEnumerable<PackagingOrderSummaryDto> Items, int TotalCount)> GetPendingOrdersAsync(
        Guid packagingStaffId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<PackagingOrderDetailDto?> GetOrderDetailAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<PackagingOrderDetailDto> ConfirmOrderAsync(
        Guid orderId,
        Guid packagingStaffId,
        ConfirmPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PackagingOrderDetailDto> MarkCollectedAsync(
        Guid orderId,
        Guid packagingStaffId,
        CollectPackagingOrderRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PackagingOrderDetailDto> CompletePackagingAsync(
        Guid orderId,
        Guid packagingStaffId,
        CompletePackagingOrderRequestDto request,
        CancellationToken cancellationToken = default);
}
