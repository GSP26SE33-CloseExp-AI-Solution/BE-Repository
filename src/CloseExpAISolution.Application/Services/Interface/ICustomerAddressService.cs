using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICustomerAddressService
{
    Task<IEnumerable<CustomerAddressResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto?> GetByIdAsync(Guid customerAddressId, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto> CreateAsync(Guid userId, UpsertCustomerAddressRequestDto request, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto?> UpdateAsync(Guid customerAddressId, Guid userId, UpsertCustomerAddressRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid customerAddressId, Guid userId, CancellationToken cancellationToken = default);
}
