using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IOrderService
{
    Task<IEnumerable<DeliveryTimeSlotDto>> GetDeliveryTimeSlotsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PickupPointDto>> GetCollectionPointsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PickupPointDto>> GetCollectionPointsNearbyAsync(
        NearbyCollectionPointsRequestDto request,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<CustomerAddressDto>> GetCustomerAddressesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<OrderResponseDto> Items, int TotalCount)> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<OrderResponseDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponseDto?> GetByIdWithDetailsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderResponseDto> CreateAsync(CreateOrderRequestDto request, CancellationToken cancellationToken = default);
    Task<OrderResponseDto> CreateForCustomerAsync(Guid userId, CreateOwnOrderRequestDto request, CancellationToken cancellationToken = default);
    Task<(IEnumerable<OrderResponseDto> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid orderId, UpdateOrderRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid orderId, OrderState status, CancellationToken cancellationToken = default);
    Task<OrderResponseDto> ApplyPromotionAsync(Guid orderId, Guid userId, ApplyPromotionToOrderRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid orderId, CancellationToken cancellationToken = default);
}
