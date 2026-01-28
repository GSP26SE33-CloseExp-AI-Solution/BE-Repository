using System.Linq.Expressions;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public abstract class BaseCrudService<T> : IBaseCrudService<T> where T : class
{
    protected BaseCrudService(IUnitOfWork unitOfWork, IGenericRepository<T> repository)
    {
        UnitOfWork = unitOfWork;
        Repository = repository;
    }

    protected IUnitOfWork UnitOfWork { get; }
    protected IGenericRepository<T> Repository { get; }

    public Task<T?> GetByIdAsync(int id) => Repository.GetByIdAsync(id);
    public Task<IEnumerable<T>> GetAllAsync() => Repository.GetAllAsync();
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Repository.FindAsync(predicate);
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) => Repository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) => Repository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate) => Repository.ExistsAsync(predicate);

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        var added = await Repository.AddAsync(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var added = await Repository.AddRangeAsync(entities);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        Repository.UpdateRange(entities);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        Repository.Delete(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        Repository.DeleteRange(entities);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

