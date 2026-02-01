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
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return null;

        Enum.TryParse<ProductState>(product.Status, out var status);
        return new ProductResponseDto
        {
            ProductId = product.ProductId,
            SupermarketId = product.SupermarketId,
            Name = product.Name,
            Brand = product.Brand,
            Category = product.Category,
            Barcode = product.Barcode,
            ManufactureDate = product.ManufactureDate,
            ExpiryDate = product.ExpiryDate,
            OriginalPrice = product.OriginalPrice,
            SuggestedPrice = product.SuggestedPrice,
            FinalPrice = product.FinalPrice,
            Status = status,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            ProductImages = product.ProductImages
        };
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync()
    {
        var products = await _context.Products
            .Include(p => p.ProductImages)
            .ToListAsync();

        return products.Select(p =>
        {
            Enum.TryParse<ProductState>(p.Status, out var status);
            return new ProductResponseDto
            {
                ProductId = p.ProductId,
                SupermarketId = p.SupermarketId,
                Name = p.Name,
                Brand = p.Brand,
                Category = p.Category,
                Barcode = p.Barcode,
                ManufactureDate = p.ManufactureDate,
                ExpiryDate = p.ExpiryDate,
                OriginalPrice = p.OriginalPrice,
                SuggestedPrice = p.SuggestedPrice,
                FinalPrice = p.FinalPrice,
                Status = status,
                CreatedBy = p.CreatedBy,
                CreatedAt = p.CreatedAt,
                ProductImages = p.ProductImages
            };
        });
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            SupermarketId = request.SupermarketId,
            Name = request.Name,
            Brand = request.Brand,
            Category = request.Category,
            Barcode = request.Barcode,
            ManufactureDate = request.ManufactureDate,
            ExpiryDate = request.ExpiryDate,
            OriginalPrice = request.OriginalPrice,
            SuggestedPrice = request.SuggestedPrice,
            FinalPrice = request.SuggestedPrice,
            Status = ProductState.Hidden.ToString(),
            CreatedBy = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        var added = await _unitOfWork.ProductRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Enum.TryParse<ProductState>(added.Status, out var status);
        return new ProductResponseDto
        {
            ProductId = added.ProductId,
            SupermarketId = added.SupermarketId,
            Name = added.Name,
            Brand = added.Brand,
            Category = added.Category,
            Barcode = added.Barcode,
            ManufactureDate = added.ManufactureDate,
            ExpiryDate = added.ExpiryDate,
            OriginalPrice = added.OriginalPrice,
            SuggestedPrice = added.SuggestedPrice,
            FinalPrice = added.FinalPrice,
            Status = status,
            CreatedBy = added.CreatedBy,
            CreatedAt = added.CreatedAt,
            ProductImages = added.ProductImages
        };
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);

        if (product == null) throw new KeyNotFoundException($"Product with id {id} not found");

        product.SupermarketId = request.SupermarketId;
        product.Name = request.Name;
        product.Brand = request.Brand;
        product.Category = request.Category;
        product.Barcode = request.Barcode;
        product.ManufactureDate = request.ManufactureDate;
        product.ExpiryDate = request.ExpiryDate;
        product.OriginalPrice = request.OriginalPrice;
        product.SuggestedPrice = request.SuggestedPrice;
        product.Status = request.Status.ToString();

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

