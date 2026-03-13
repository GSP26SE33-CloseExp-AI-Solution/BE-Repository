using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IProductService
{
    Task<Product?> GetByIdAsync(int id);
    Task<IEnumerable<Product>> GetAllAsync();
    Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate);
    Task<Product?> FirstOrDefaultAsync(Expression<Func<Product, bool>> predicate);
    Task<Product> CreateAsync(Product entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Product entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Product, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<Product, bool>> predicate);
    Task<ProductResponseDto?> GetByIdWithImagesAsync(Guid id);
    Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync();
    Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Guid id, UpdateProductRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockLotDetailDto> Items, int TotalCount)> GetStockLotsBySupermarketAsync(StockLotFilterDto filter);
    Task<(IEnumerable<ProductResponseDto> Items, int TotalCount)> GetProductsBySupermarketAsync(Guid supermarketId, string? searchTerm = null, string? category = null, int pageNumber = 1, int pageSize = 20);
    Task<ProductDetailDto?> GetProductDetailAsync(Guid productId);
}