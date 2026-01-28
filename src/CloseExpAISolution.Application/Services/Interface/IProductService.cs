using System.Linq.Expressions;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate);
    Task<Product?> FirstOrDefaultAsync(Expression<Func<Product, bool>> predicate);
    Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Product, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<Product, bool>> predicate);
}

