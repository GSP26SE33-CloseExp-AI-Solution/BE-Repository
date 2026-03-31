using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICustomerAddressService
{
    Task<IEnumerable<CustomerAddressResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto?> GetByIdAsync(Guid customerAddressId, CancellationToken cancellationToken = default);
    Task<CustomerAddressResponseDto?> GetDefaultAddressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<CustomerAddressResponseDto>> CreateAsync(Guid userId, CreateCustomerAddressDto request);
    Task<ApiResponse<CustomerAddressResponseDto>> UpdateAsync(Guid userId, Guid addressId, UpdateCustomerAddressDto request);
    Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid addressId);
    Task<ApiResponse<bool>> SetDefaultAsync(Guid userId, Guid addressId);
}
