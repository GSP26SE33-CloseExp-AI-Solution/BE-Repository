using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICustomerAddressService
{
    Task<IEnumerable<CustomerAddressResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto?> GetByIdAsync(Guid customerAddressId, CancellationToken cancellationToken = default);
}
