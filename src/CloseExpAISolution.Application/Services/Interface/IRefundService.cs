using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IRefundService
{
    Task<(IEnumerable<RefundResponseDto> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<RefundResponseDto?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default);
    Task<RefundResponseDto> CreateAsync(CreateRefundRequestDto request, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Guid refundId, RefundState status, string? processedBy, CancellationToken cancellationToken = default);
}
