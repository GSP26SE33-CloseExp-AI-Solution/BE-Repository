using System.Linq.Expressions;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IProductImageService
{
    Task<ProductImage?> GetByIdAsync(int id);
    Task<IEnumerable<ProductImage>> GetAllAsync();
    Task<IEnumerable<ProductImage>> FindAsync(Expression<Func<ProductImage, bool>> predicate);
    Task<ProductImage?> FirstOrDefaultAsync(Expression<Func<ProductImage, bool>> predicate);
    Task<ProductImage> AddAsync(ProductImage entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductImage>> AddRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(ProductImage entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(ProductImage entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<ProductImage, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<ProductImage, bool>> predicate);
}

