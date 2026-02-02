using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork unitOfWork, ApplicationDbContext context, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _mapper = mapper;
    }

    public Task<Product?> GetByIdAsync(int id) => _unitOfWork.ProductRepository.GetByIdAsync(id);
    public Task<IEnumerable<Product>> GetAllAsync() => _unitOfWork.ProductRepository.GetAllAsync();
    public Task<IEnumerable<Product>> FindAsync(Expression<Func<Product, bool>> predicate) => _unitOfWork.ProductRepository.FindAsync(predicate);
    public Task<Product?> FirstOrDefaultAsync(Expression<Func<Product, bool>> predicate) => _unitOfWork.ProductRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<Product, bool>>? predicate = null) => _unitOfWork.ProductRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<Product, bool>> predicate) => _unitOfWork.ProductRepository.ExistsAsync(predicate);

    public async Task<Product> CreateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.ProductRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<Product>> AddRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.ProductRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Product entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<Product> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.ProductRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductResponseDto?> GetByIdWithImagesAsync(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Pricing)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return null;

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync()
    {
        var products = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Pricing)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = _mapper.Map<Product>(request);

        var added = await _unitOfWork.ProductRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ProductResponseDto>(added);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Pricing)
            .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);

        if (product == null) throw new KeyNotFoundException($"Không tìm thấy sản phẩm với id {id}");

        _mapper.Map(request, product);

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == id);
        if (product == null) throw new KeyNotFoundException($"Không tìm thấy sản phẩm với id {id}");

        await DeleteAsync(product, cancellationToken);
    }
}

