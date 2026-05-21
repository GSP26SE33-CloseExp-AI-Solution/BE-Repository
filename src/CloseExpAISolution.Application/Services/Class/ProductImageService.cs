using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductImageService : IProductImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ProductImageService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
        return primary == null ? null : _mapper.Map<CustomerProductImageResponseDto>(primary);
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

        return _mapper.Map<IEnumerable<CustomerProductImageResponseDto>>(images);
    }

    private async Task EnsureProductVisibleToCustomerAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
            throw new KeyNotFoundException("Không tìm thấy sản phẩm");

        if (product.Status == ProductState.Hidden || product.Status == ProductState.Deleted)
            throw new KeyNotFoundException("Không tìm thấy sản phẩm");
    }

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

