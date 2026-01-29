using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class MarketStaffService : IMarketStaffService
{
    private readonly IUnitOfWork _unitOfWork;

    public MarketStaffService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<MarketStaff?> GetByIdAsync(int id) => _unitOfWork.MarketStaffRepository.GetByIdAsync(id);
    public Task<IEnumerable<MarketStaff>> GetAllAsync() => _unitOfWork.MarketStaffRepository.GetAllAsync();
    public Task<IEnumerable<MarketStaff>> FindAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.FindAsync(predicate);
    public Task<MarketStaff?> FirstOrDefaultAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<MarketStaff, bool>>? predicate = null) => _unitOfWork.MarketStaffRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<MarketStaff, bool>> predicate) => _unitOfWork.MarketStaffRepository.ExistsAsync(predicate);

    public async Task<MarketStaff> AddAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.MarketStaffRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<MarketStaff>> AddRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.MarketStaffRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MarketStaff entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.MarketStaffRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<MarketStaffResponseDto?> GetByIdWithDtoAsync(Guid id)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) return null;

        return new MarketStaffResponseDto
        {
            MarketStaffId = marketStaff.MarketStaffId,
            UserId = marketStaff.UserId,
            SupermarketId = marketStaff.SupermarketId,
            Position = marketStaff.Position,
            CreatedAt = marketStaff.CreatedAt
        };
    }

    public async Task<IEnumerable<MarketStaffResponseDto>> GetAllWithDtoAsync()
    {
        var items = await _unitOfWork.MarketStaffRepository.GetAllAsync();
        return items.Select(x => new MarketStaffResponseDto
        {
            MarketStaffId = x.MarketStaffId,
            UserId = x.UserId,
            SupermarketId = x.SupermarketId,
            Position = x.Position,
            CreatedAt = x.CreatedAt
        });
    }

    public async Task<MarketStaffResponseDto> CreateMarketStaffAsync(CreateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = new MarketStaff
        {
            MarketStaffId = Guid.NewGuid(),
            UserId = request.UserId,
            SupermarketId = request.SupermarketId,
            Position = request.Position,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.MarketStaffRepository.AddAsync(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new MarketStaffResponseDto
        {
            MarketStaffId = added.MarketStaffId,
            UserId = added.UserId,
            SupermarketId = added.SupermarketId,
            Position = added.Position,
            CreatedAt = added.CreatedAt
        };
    }

    public async Task UpdateMarketStaffAsync(Guid id, UpdateMarketStaffRequestDto request, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"MarketStaff with id {id} not found");

        marketStaff.UserId = request.UserId;
        marketStaff.SupermarketId = request.SupermarketId;
        marketStaff.Position = request.Position;

        _unitOfWork.MarketStaffRepository.Update(marketStaff);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMarketStaffAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var marketStaff = await _unitOfWork.MarketStaffRepository.FirstOrDefaultAsync(x => x.MarketStaffId == id);
        if (marketStaff == null) throw new KeyNotFoundException($"MarketStaff with id {id} not found");

        await DeleteAsync(marketStaff, cancellationToken);
    }
}

