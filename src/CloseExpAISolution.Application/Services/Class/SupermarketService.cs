using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class SupermarketService : ISupermarketService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupermarketService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Supermarket?> GetByIdAsync(int id) => _unitOfWork.SupermarketRepository.GetByIdAsync(id);
    public Task<IEnumerable<Supermarket>> GetAllAsync() => _unitOfWork.SupermarketRepository.GetAllAsync();
    public Task<IEnumerable<Supermarket>> FindAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.FindAsync(predicate);
    public Task<Supermarket?> FirstOrDefaultAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<Supermarket, bool>>? predicate = null) => _unitOfWork.SupermarketRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<Supermarket, bool>> predicate) => _unitOfWork.SupermarketRepository.ExistsAsync(predicate);

    public async Task<Supermarket> AddAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<Supermarket>> AddRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.SupermarketRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Supermarket entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.SupermarketRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SupermarketResponseDto?> GetByIdWithDtoAsync(Guid id)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) return null;

        Enum.TryParse<UserState>(supermarket.Status, out var status);
        return new SupermarketResponseDto
        {
            SupermarketId = supermarket.SupermarketId,
            Name = supermarket.Name,
            Address = supermarket.Address,
            Latitude = supermarket.Latitude,
            Longitude = supermarket.Longitude,
            ContactPhone = supermarket.ContactPhone,
            Status = status,
            CreatedAt = supermarket.CreatedAt
        };
    }

    public async Task<IEnumerable<SupermarketResponseDto>> GetAllWithDtoAsync()
    {
        var items = await _unitOfWork.SupermarketRepository.GetAllAsync();
        return items.Select(x =>
        {
            Enum.TryParse<UserState>(x.Status, out var status);
            return new SupermarketResponseDto
            {
                SupermarketId = x.SupermarketId,
                Name = x.Name,
                Address = x.Address,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                ContactPhone = x.ContactPhone,
                Status = status,
                CreatedAt = x.CreatedAt
            };
        });
    }

    public async Task<SupermarketResponseDto> CreateSupermarketAsync(CreateSupermarketRequestDto request, CancellationToken cancellationToken = default)
    {
        var supermarket = new Supermarket
        {
            SupermarketId = Guid.NewGuid(),
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ContactPhone = request.ContactPhone,
            Status = UserState.Active.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.SupermarketRepository.AddAsync(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Enum.TryParse<UserState>(added.Status, out var status);
        return new SupermarketResponseDto
        {
            SupermarketId = added.SupermarketId,
            Name = added.Name,
            Address = added.Address,
            Latitude = added.Latitude,
            Longitude = added.Longitude,
            ContactPhone = added.ContactPhone,
            Status = status,
            CreatedAt = added.CreatedAt
        };
    }

    public async Task UpdateSupermarketAsync(Guid id, UpdateSupermarketRequestDto request, CancellationToken cancellationToken = default)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) throw new KeyNotFoundException($"Supermarket with id {id} not found");

        supermarket.Name = request.Name;
        supermarket.Address = request.Address;
        supermarket.Latitude = request.Latitude;
        supermarket.Longitude = request.Longitude;
        supermarket.ContactPhone = request.ContactPhone;
        supermarket.Status = request.Status.ToString();

        _unitOfWork.SupermarketRepository.Update(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSupermarketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(x => x.SupermarketId == id);
        if (supermarket == null) throw new KeyNotFoundException($"Supermarket with id {id} not found");

        await DeleteAsync(supermarket, cancellationToken);
    }
}

