using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class CustomerAddressService : ICustomerAddressService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapboxService _mapboxService;
    private readonly ILogger<CustomerAddressService> _logger;

    public CustomerAddressService(
        IUnitOfWork unitOfWork,
        IMapboxService mapboxService,
        ILogger<CustomerAddressService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapboxService = mapboxService;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerAddressResponseDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _unitOfWork.Repository<CustomerAddress>()
            .FindAsync(x => x.UserId == userId);

        return addresses
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.RecipientName)
            .Select(MapToDto);
    }

    public async Task<CustomerAddressResponseDto?> GetByIdAsync(Guid customerAddressId, CancellationToken cancellationToken = default)
    {
        var address = await _unitOfWork.Repository<CustomerAddress>()
            .FirstOrDefaultAsync(x => x.CustomerAddressId == customerAddressId);

        return address == null ? null : MapToDto(address);
    }

    public async Task<CustomerAddressResponseDto?> GetDefaultAddressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var address = await _unitOfWork.Repository<CustomerAddress>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.IsDefault);

        return address == null ? null : MapToDto(address);
    }

    public async Task<ApiResponse<CustomerAddressResponseDto>> CreateAsync(Guid userId, CreateCustomerAddressDto request)
    {
        var repo = _unitOfWork.Repository<CustomerAddress>();

        // Auto-geocode nếu không truyền tọa độ
        var latitude = request.Latitude;
        var longitude = request.Longitude;
        if (latitude == 0 && longitude == 0 && !string.IsNullOrWhiteSpace(request.AddressLine))
        {
            var geocodeResult = await TryGeocodeAddressAsync(request.AddressLine);
            if (geocodeResult.HasValue)
            {
                latitude = geocodeResult.Value.Latitude;
                longitude = geocodeResult.Value.Longitude;
            }
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Nếu đặt là default, bỏ default của các địa chỉ khác
            if (request.IsDefault)
            {
                await ClearDefaultAddressesAsync(userId);
            }
            else
            {
                // Nếu chưa có địa chỉ nào, tự động set default
                var existingCount = (await repo.FindAsync(x => x.UserId == userId)).Count();
                if (existingCount == 0)
                    request.IsDefault = true;
            }

            var address = new CustomerAddress
            {
                CustomerAddressId = Guid.NewGuid(),
                UserId = userId,
                Phone = request.Phone,
                RecipientName = request.RecipientName,
                AddressLine = request.AddressLine,
                Latitude = latitude,
                Longitude = longitude,
                IsDefault = request.IsDefault
            };

            await repo.AddAsync(address);
            await _unitOfWork.CommitTransactionAsync();

            return ApiResponse<CustomerAddressResponseDto>.SuccessResponse(MapToDto(address), "Thêm địa chỉ thành công");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ApiResponse<CustomerAddressResponseDto>> UpdateAsync(Guid userId, Guid addressId, UpdateCustomerAddressDto request)
    {
        var repo = _unitOfWork.Repository<CustomerAddress>();
        var address = await repo.FirstOrDefaultAsync(x => x.CustomerAddressId == addressId);

        if (address == null)
            return ApiResponse<CustomerAddressResponseDto>.ErrorResponse("Không tìm thấy địa chỉ");

        if (address.UserId != userId)
            return ApiResponse<CustomerAddressResponseDto>.ErrorResponse("Bạn không có quyền cập nhật địa chỉ này");

        if (!string.IsNullOrEmpty(request.Phone))
            address.Phone = request.Phone;

        if (!string.IsNullOrEmpty(request.RecipientName))
            address.RecipientName = request.RecipientName;

        // Nếu đổi AddressLine và không truyền lat/long mới → auto-geocode
        if (!string.IsNullOrEmpty(request.AddressLine) && request.AddressLine != address.AddressLine)
        {
            address.AddressLine = request.AddressLine;

            if (!request.Latitude.HasValue && !request.Longitude.HasValue)
            {
                var geocodeResult = await TryGeocodeAddressAsync(request.AddressLine);
                if (geocodeResult.HasValue)
                {
                    address.Latitude = geocodeResult.Value.Latitude;
                    address.Longitude = geocodeResult.Value.Longitude;
                }
            }
        }

        if (request.Latitude.HasValue)
            address.Latitude = request.Latitude.Value;

        if (request.Longitude.HasValue)
            address.Longitude = request.Longitude.Value;

        repo.Update(address);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<CustomerAddressResponseDto>.SuccessResponse(MapToDto(address), "Cập nhật địa chỉ thành công");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid addressId)
    {
        var repo = _unitOfWork.Repository<CustomerAddress>();
        var address = await repo.FirstOrDefaultAsync(x => x.CustomerAddressId == addressId);

        if (address == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy địa chỉ");

        if (address.UserId != userId)
            return ApiResponse<bool>.ErrorResponse("Bạn không có quyền xóa địa chỉ này");

        var wasDefault = address.IsDefault;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            repo.Delete(address);

            // Nếu xóa địa chỉ default, tự động set địa chỉ khác làm default
            if (wasDefault)
            {
                var remaining = await repo.FirstOrDefaultAsync(x => x.UserId == userId);
                if (remaining != null)
                {
                    remaining.IsDefault = true;
                    repo.Update(remaining);
                }
            }

            await _unitOfWork.CommitTransactionAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Xóa địa chỉ thành công");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<ApiResponse<bool>> SetDefaultAsync(Guid userId, Guid addressId)
    {
        var repo = _unitOfWork.Repository<CustomerAddress>();
        var address = await repo.FirstOrDefaultAsync(x => x.CustomerAddressId == addressId);

        if (address == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy địa chỉ");

        if (address.UserId != userId)
            return ApiResponse<bool>.ErrorResponse("Bạn không có quyền thay đổi địa chỉ này");

        if (address.IsDefault)
            return ApiResponse<bool>.SuccessResponse(true, "Địa chỉ này đã là mặc định");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await ClearDefaultAddressesAsync(userId);

            address.IsDefault = true;
            repo.Update(address);

            await _unitOfWork.CommitTransactionAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Đặt địa chỉ mặc định thành công");
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task ClearDefaultAddressesAsync(Guid userId)
    {
        var repo = _unitOfWork.Repository<CustomerAddress>();
        var defaultAddresses = await repo.FindAsync(x => x.UserId == userId && x.IsDefault);
        foreach (var addr in defaultAddresses)
        {
            addr.IsDefault = false;
            repo.Update(addr);
        }
    }

    private async Task<(decimal Latitude, decimal Longitude)?> TryGeocodeAddressAsync(string addressLine)
    {
        try
        {
            var result = await _mapboxService.ForwardGeocodeAsync(addressLine);
            if (result != null)
            {
                return ((decimal)result.Latitude, (decimal)result.Longitude);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to geocode address: {Address}", addressLine);
        }
        return null;
    }

    private static CustomerAddressResponseDto MapToDto(CustomerAddress x) => new()
    {
        CustomerAddressId = x.CustomerAddressId,
        UserId = x.UserId,
        Phone = x.Phone,
        RecipientName = x.RecipientName,
        AddressLine = x.AddressLine,
        Latitude = x.Latitude,
        Longitude = x.Longitude,
        IsDefault = x.IsDefault
    };
}
