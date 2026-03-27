using CloseExpAISolution.Application.DTOs.Request;
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

    public async Task<CustomerAddressResponseDto> CreateAsync(Guid userId, UpsertCustomerAddressRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.IsDefault)
            await ClearDefaultAddressesAsync(userId, cancellationToken);

        var entity = new CustomerAddress
        {
            CustomerAddressId = Guid.NewGuid(),
            UserId = userId,
            Phone = request.Phone.Trim(),
            RecipientName = request.RecipientName.Trim(),
            AddressLine = request.AddressLine.Trim(),
            IsDefault = request.IsDefault
        };

        await _unitOfWork.Repository<CustomerAddress>().AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.CustomerAddressId, cancellationToken) ?? throw new InvalidOperationException("Cannot create address.");
    }

    public async Task<CustomerAddressResponseDto?> UpdateAsync(Guid customerAddressId, Guid userId, UpsertCustomerAddressRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<CustomerAddress>()
            .FirstOrDefaultAsync(x => x.CustomerAddressId == customerAddressId && x.UserId == userId);
        if (entity == null)
            return null;

        if (request.IsDefault)
            await ClearDefaultAddressesAsync(userId, cancellationToken);

        entity.Phone = request.Phone.Trim();
        entity.RecipientName = request.RecipientName.Trim();
        entity.AddressLine = request.AddressLine.Trim();
        entity.IsDefault = request.IsDefault;

        _unitOfWork.Repository<CustomerAddress>().Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.CustomerAddressId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid customerAddressId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Repository<CustomerAddress>()
            .FirstOrDefaultAsync(x => x.CustomerAddressId == customerAddressId && x.UserId == userId);
        if (entity == null)
            return false;

        _unitOfWork.Repository<CustomerAddress>().Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ClearDefaultAddressesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var defaults = (await _unitOfWork.Repository<CustomerAddress>()
            .FindAsync(x => x.UserId == userId && x.IsDefault))
            .ToList();

        if (defaults.Count == 0) return;

        foreach (var item in defaults)
            item.IsDefault = false;

        _unitOfWork.Repository<CustomerAddress>().UpdateRange(defaults);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
