using System.Linq.Expressions;
using System.Text.Json;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
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

        return _mapper.Map<ProductResponseDto>(product);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync()
    {
        var products = await _context.Products
            .Include(p => p.ProductImages)
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

    public async Task<(IEnumerable<ProductLotDetailDto> Items, int TotalCount)> GetProductLotsBySupermarketAsync(ProductLotFilterDto filter)
    {
        var query = _context.ProductLots
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.Supermarket)
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.ProductImages)
            .Include(pl => pl.Unit)
            .AsQueryable();

        // Filter theo SupermarketId
        if (filter.SupermarketId.HasValue)
        {
            query = query.Where(pl => pl.Product!.SupermarketId == filter.SupermarketId.Value);
        }

        // Filter theo WeightType
        if (filter.WeightType.HasValue)
        {
            query = query.Where(pl => pl.Product!.WeightType == (int)filter.WeightType.Value);
        }

        // Filter theo IsFreshFood
        if (filter.IsFreshFood.HasValue)
        {
            query = query.Where(pl => pl.Product!.IsFreshFood == filter.IsFreshFood.Value);
        }

        // Filter theo Category
        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(pl => pl.Product!.Category == filter.Category);
        }

        // Search theo tên sản phẩm
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(pl =>
                pl.Product!.Name.ToLower().Contains(searchLower) ||
                pl.Product!.Brand.ToLower().Contains(searchLower) ||
                pl.Product!.Barcode.Contains(searchLower)
            );
        }

        // Load data
        var lots = await query.ToListAsync();
        var now = DateTime.UtcNow;

        // Map và tính toán expiry status
        var lotDtos = lots.Select(pl =>
        {
            // Lấy danh sách ảnh và sắp xếp theo thời gian upload
            var productImages = pl.Product?.ProductImages?
                .OrderBy(img => img.UploadedAt)
                .Select(img => new ProductImageDto
                {
                    ProductImageId = img.ProductImageId,
                    ProductId = img.ProductId,
                    ImageUrl = img.ImageUrl,
                    UploadedAt = img.UploadedAt
                })
                .ToList() ?? new List<ProductImageDto>();

            var dto = new ProductLotDetailDto
            {
                LotId = pl.LotId,
                ProductId = pl.ProductId,
                ExpiryDate = pl.ExpiryDate,
                ManufactureDate = pl.ManufactureDate,
                Quantity = pl.Quantity,
                Weight = pl.Weight,
                Status = pl.Status,
                UnitId = pl.UnitId,
                UnitName = pl.Unit?.Name ?? "",
                UnitType = pl.Unit?.Type ?? "",
                OriginalUnitPrice = pl.OriginalUnitPrice,
                SuggestedUnitPrice = pl.SuggestedUnitPrice,
                FinalUnitPrice = pl.FinalUnitPrice,
                ProductName = pl.Product?.Name ?? "",
                Brand = pl.Product?.Brand ?? "",
                Category = pl.Product?.Category ?? "",
                Barcode = pl.Product?.Barcode ?? "",
                IsFreshFood = pl.Product?.IsFreshFood ?? false,
                WeightType = (ProductWeightType)(pl.Product?.WeightType ?? 1),
                WeightTypeName = GetWeightTypeName(pl.Product?.WeightType ?? 1),
                DefaultPricePerKg = pl.Product?.DefaultPricePerKg,
                SupermarketId = pl.Product?.SupermarketId ?? Guid.Empty,
                SupermarketName = pl.Product?.Supermarket?.Name ?? "",
                // Thông tin ảnh
                MainImageUrl = productImages.FirstOrDefault()?.ImageUrl,
                TotalImages = productImages.Count,
                ProductImages = productImages,
                // Thông tin thành phần & dinh dưỡng
                Ingredients = pl.Product?.Ingredients,
                NutritionFacts = ParseNutritionFacts(pl.Product?.NutritionFactsJson),
                CreatedAt = pl.CreatedAt
            };

            // Tính toán trạng thái hạn sử dụng
            var timeRemaining = pl.ExpiryDate - now;
            dto.DaysRemaining = (int)Math.Floor(timeRemaining.TotalDays);

            if (timeRemaining.TotalDays < 0)
            {
                // Đã hết hạn
                dto.ExpiryStatus = ExpiryStatus.Expired;
                dto.ExpiryStatusText = $"Quá hạn {Math.Abs(dto.DaysRemaining)} ngày";
            }
            else if (timeRemaining.TotalDays < 1)
            {
                // Trong ngày - đếm giờ
                dto.ExpiryStatus = ExpiryStatus.Today;
                dto.HoursRemaining = (int)Math.Floor(timeRemaining.TotalHours);
                dto.ExpiryStatusText = dto.HoursRemaining > 0
                    ? $"Còn {dto.HoursRemaining} giờ"
                    : "Hết hạn trong vài phút";
            }
            else if (timeRemaining.TotalDays <= 2)
            {
                // Sắp hết hạn (1-2 ngày)
                dto.ExpiryStatus = ExpiryStatus.ExpiringSoon;
                dto.ExpiryStatusText = $"Sắp hết hạn ({dto.DaysRemaining} ngày)";
            }
            else if (timeRemaining.TotalDays <= 7)
            {
                // Còn ngắn hạn (3-7 ngày)
                dto.ExpiryStatus = ExpiryStatus.ShortTerm;
                dto.ExpiryStatusText = $"Còn ngắn hạn ({dto.DaysRemaining} ngày)";
            }
            else
            {
                // Còn dài hạn (8+ ngày)
                dto.ExpiryStatus = ExpiryStatus.LongTerm;
                dto.ExpiryStatusText = $"Còn dài hạn ({dto.DaysRemaining} ngày)";
            }

            return dto;
        }).ToList();

        // Filter theo ExpiryStatus (sau khi tính toán)
        if (filter.ExpiryStatus.HasValue)
        {
            lotDtos = lotDtos.Where(dto => dto.ExpiryStatus == filter.ExpiryStatus.Value).ToList();
        }

        // Sort theo trạng thái hạn sử dụng
        // Priority: Today → ExpiringSoon → ShortTerm → LongTerm → Expired (cuối cùng)
        lotDtos = lotDtos
            .OrderBy(dto => dto.ExpiryStatus == ExpiryStatus.Expired ? 99 : (int)dto.ExpiryStatus)
            .ThenBy(dto => dto.ExpiryDate)
            .ToList();

        var totalCount = lotDtos.Count;

        // Pagination
        var pagedLots = lotDtos
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return (pagedLots, totalCount);
    }

    public async Task<(IEnumerable<ProductResponseDto> Items, int TotalCount)> GetProductsBySupermarketAsync(
        Guid supermarketId,
        string? searchTerm = null,
        string? category = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Supermarket)
            .Where(p => p.SupermarketId == supermarketId)
            .AsQueryable();

        // Search theo tên, brand hoặc barcode
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.Brand.ToLower().Contains(searchLower) ||
                p.Barcode.Contains(searchLower)
            );
        }

        // Filter theo category
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category == category);
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productDtos = _mapper.Map<IEnumerable<ProductResponseDto>>(products);

        return (productDtos, totalCount);
    }

    private static string GetWeightTypeName(int weightType)
    {
        return weightType switch
        {
            1 => "Định lượng cố định",
            2 => "Bán theo cân",
            _ => "Định lượng cố định"
        };
    }

    /// <summary>
    /// Parse chuỗi JSON thành Dictionary chứa thông tin dinh dưỡng
    /// </summary>
    private static Dictionary<string, string>? ParseNutritionFacts(string? nutritionFactsJson)
    {
        if (string.IsNullOrEmpty(nutritionFactsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(nutritionFactsJson);
        }
        catch
        {
            // Nếu parse lỗi, trả về null
            return null;
        }
    }
}

