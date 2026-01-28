using System.Linq.Expressions;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
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
}

