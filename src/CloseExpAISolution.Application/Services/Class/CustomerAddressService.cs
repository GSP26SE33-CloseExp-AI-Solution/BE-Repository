using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class CustomerAddressService : ICustomerAddressService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerAddressService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CustomerAddressResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Repository<CustomerAddress>()
            .FindAsync(x => x.UserId == userId);

        return addresses
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.RecipientName)
            .Select(x => new CustomerAddressResponseDto
            {
                CustomerAddressId = x.CustomerAddressId,
                UserId = x.UserId,
                Phone = x.Phone,
                RecipientName = x.RecipientName,
                AddressLine = x.AddressLine,
                IsDefault = x.IsDefault
            });
    }

    public async Task<CustomerAddressResponseDto?> GetByIdAsync(Guid customerAddressId, CancellationToken cancellationToken = default)
    {
        var address = await _unitOfWork.Repository<CustomerAddress>()
            .FirstOrDefaultAsync(x => x.CustomerAddressId == customerAddressId);

        if (address == null)
            return null;

        return new CustomerAddressResponseDto
        {
            CustomerAddressId = address.CustomerAddressId,
            UserId = address.UserId,
            Phone = address.Phone,
            RecipientName = address.RecipientName,
            AddressLine = address.AddressLine,
            IsDefault = address.IsDefault
        };
    }
}
