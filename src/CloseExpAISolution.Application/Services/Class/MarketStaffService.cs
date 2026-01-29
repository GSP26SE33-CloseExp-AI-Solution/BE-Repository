using System.Linq.Expressions;
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
}

