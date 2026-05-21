using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductImageService : IProductImageService
{
    private static readonly TimeSpan PresignExpiry = TimeSpan.FromHours(1);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2StorageService _r2Storage;

    public ProductImageService(IUnitOfWork unitOfWork, IR2StorageService r2Storage)
    {
        _unitOfWork = unitOfWork;
        _r2Storage = r2Storage;
    }

    public async Task<CustomerProductImageResponseDto?> GetPrimaryImageForCustomerAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductVisibleToCustomerAsync(productId, cancellationToken);

        var images = (await _unitOfWork.ProductImageRepository.FindAsync(pi => pi.ProductId == productId))
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.CreatedAt)
            .ToList();

        var primary = images.FirstOrDefault();
        return primary == null ? null : MapToCustomerResponse(primary);
    }

    public async Task<IEnumerable<CustomerProductImageResponseDto>> GetImagesForCustomerAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductVisibleToCustomerAsync(productId, cancellationToken);

        var images = (await _unitOfWork.ProductImageRepository.FindAsync(pi => pi.ProductId == productId))
            .OrderByDescending(pi => pi.IsPrimary)
            .ThenBy(pi => pi.CreatedAt)
            .ToList();

        return images.Select(MapToCustomerResponse);
    }

    private async Task EnsureProductVisibleToCustomerAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
            throw new KeyNotFoundException("Không tìm thấy sản phẩm");

        if (product.Status == ProductState.Hidden || product.Status == ProductState.Deleted)
            throw new KeyNotFoundException("Không tìm thấy sản phẩm");
    }

    private CustomerProductImageResponseDto MapToCustomerResponse(ProductImage image) =>
        new()
        {
            ProductImageId = image.ProductImageId,
            ProductId = image.ProductId,
            ImageUrl = image.ImageUrl,
            PreSignedUrl = _r2Storage.GetPreSignedUrlForImage(image.ImageUrl, PresignExpiry),
            IsPrimary = image.IsPrimary,
            CreatedAt = image.CreatedAt
        };

    public Task<ProductImage?> GetByIdAsync(int id) => _unitOfWork.ProductImageRepository.GetByIdAsync(id);
    public Task<IEnumerable<ProductImage>> GetAllAsync() => _unitOfWork.ProductImageRepository.GetAllAsync();
    public Task<IEnumerable<ProductImage>> FindAsync(Expression<Func<ProductImage, bool>> predicate) => _unitOfWork.ProductImageRepository.FindAsync(predicate);
    public Task<ProductImage?> FirstOrDefaultAsync(Expression<Func<ProductImage, bool>> predicate) => _unitOfWork.ProductImageRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<ProductImage, bool>>? predicate = null) => _unitOfWork.ProductImageRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<ProductImage, bool>> predicate) => _unitOfWork.ProductImageRepository.ExistsAsync(predicate);

    public async Task<ProductImage> AddAsync(ProductImage entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.ProductImageRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<ProductImage>> AddRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.ProductImageRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(ProductImage entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductImageRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductImageRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(ProductImage entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductImageRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<ProductImage> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductImageRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

