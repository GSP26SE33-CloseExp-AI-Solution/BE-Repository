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
            .Include(p => p.ProductDetail)
            .Include(p => p.Unit)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return null;

        var dto = _mapper.Map<ProductResponseDto>(product);
        var images = await _context.ProductImages
            .Where(x => x.ProductId == id)
            .OrderBy(i => i.UploadedAt)
            .ToListAsync(cancellationToken: default);
        var pricing = await _context.Pricings
            .FirstOrDefaultAsync(x => x.ProductId == id, cancellationToken: default);

        dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        dto.TotalImages = images.Count;
        dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
        if (pricing != null)
        {
            dto.OriginalPrice = pricing.OriginalUnitPrice;
            dto.SuggestedPrice = pricing.SuggestedUnitPrice;
            dto.FinalPrice = pricing.FinalUnitPrice;
            dto.PricingConfidence = pricing.PricingConfidence;
            dto.PricedBy = pricing.PricedBy;
            dto.PricedAt = pricing.PricedAt;
        }
        return dto;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync()
    {
        var products = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.Unit)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();
        var imagesByProduct = await _context.ProductImages
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();
        var pricingsByProduct = await _context.Pricings
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync();

        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var pricingLookup = pricingsByProduct.ToDictionary(p => p.ProductId);

        var result = new List<ProductResponseDto>();
        foreach (var product in products)
        {
            var dto = _mapper.Map<ProductResponseDto>(product);
            var images = imageLookup.GetValueOrDefault(product.ProductId) ?? new List<ProductImage>();
            dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
            dto.TotalImages = images.Count;
            dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
            if (pricingLookup.TryGetValue(product.ProductId, out var pricing))
            {
                dto.OriginalPrice = pricing.OriginalUnitPrice;
                dto.SuggestedPrice = pricing.SuggestedUnitPrice;
                dto.FinalPrice = pricing.FinalUnitPrice;
                dto.PricingConfidence = pricing.PricingConfidence;
                dto.PricedBy = pricing.PricedBy;
                dto.PricedAt = pricing.PricedAt;
            }
            result.Add(dto);
        }
        return result;
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = _mapper.Map<Product>(request);

        var defaultUnit = await _context.Units.FirstOrDefaultAsync(cancellationToken);
        if (defaultUnit == null)
            throw new InvalidOperationException("Không tìm thấy đơn vị đo mặc định. Vui lòng seed bảng Units trước.");
        product.UnitId = defaultUnit.UnitId;

        var added = await _unitOfWork.ProductRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = added.ProductId,
            Brand = request.Brand,
            Ingredients = request.Ingredients,
            NutritionFacts = request.Nutrition,
            UsageInstructions = request.Usage,
            Manufacturer = request.Manufacturer,
            SafetyWarning = request.Warning
        };
        await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var withDetail = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.Unit)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .FirstOrDefaultAsync(p => p.ProductId == added.ProductId, cancellationToken);
        if (withDetail == null) throw new InvalidOperationException("Product not found after create.");

        var dto = _mapper.Map<ProductResponseDto>(withDetail);
        var images = await _context.ProductImages.Where(x => x.ProductId == added.ProductId).OrderBy(i => i.UploadedAt).ToListAsync(cancellationToken);
        var pricing = await _context.Pricings.FirstOrDefaultAsync(x => x.ProductId == added.ProductId, cancellationToken);
        dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        dto.TotalImages = images.Count;
        dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
        if (pricing != null)
        {
            dto.OriginalPrice = pricing.OriginalUnitPrice;
            dto.SuggestedPrice = pricing.SuggestedUnitPrice;
            dto.FinalPrice = pricing.FinalUnitPrice;
            dto.PricingConfidence = pricing.PricingConfidence;
            dto.PricedBy = pricing.PricedBy;
            dto.PricedAt = pricing.PricedAt;
        }
        return dto;
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.ProductDetail)
            .FirstOrDefaultAsync(p => p.ProductId == id, cancellationToken);

        if (product == null) throw new KeyNotFoundException($"Không tìm thấy sản phẩm với id {id}");

        _mapper.Map(request, product);

        var detail = product.ProductDetail ?? new ProductDetail { ProductDetailId = Guid.NewGuid(), ProductId = product.ProductId };
        detail.Brand = request.Brand;
        detail.Ingredients = request.Ingredients;
        detail.NutritionFacts = request.Nutrition;
        detail.UsageInstructions = request.Usage;
        detail.Manufacturer = request.Manufacturer;
        detail.SafetyWarning = request.Warning;
        if (product.ProductDetail == null)
        {
            await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
            product.ProductDetail = detail;
        }
        else
            _unitOfWork.Repository<ProductDetail>().Update(detail);

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
                .ThenInclude(p => p!.ProductDetail)
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.Unit)
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.CategoryRef)
            .AsQueryable();

        // Filter theo SupermarketId
        if (filter.SupermarketId.HasValue)
        {
            query = query.Where(pl => pl.Product!.SupermarketId == filter.SupermarketId.Value);
        }

        // Filter theo IsFreshFood (from CategoryRef)
        if (filter.IsFreshFood.HasValue)
        {
            query = query.Where(pl => pl.Product!.CategoryRef != null && pl.Product.CategoryRef.IsFreshFood == filter.IsFreshFood.Value);
        }

        // Filter theo Category (from CategoryRef.Name)
        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(pl => pl.Product!.CategoryRef != null && pl.Product.CategoryRef.Name == filter.Category);
        }

        // Search theo tên sản phẩm
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(pl =>
                pl.Product!.Name.ToLower().Contains(searchLower) ||
                (pl.Product!.ProductDetail != null && pl.Product.ProductDetail.Brand != null && pl.Product.ProductDetail.Brand.ToLower().Contains(searchLower)) ||
                pl.Product!.Barcode.Contains(searchLower)
            );
        }

        // Load data
        var lots = await query.ToListAsync();
        var now = DateTime.UtcNow;

        // Map using AutoMapper
        var lotDtos = _mapper.Map<List<ProductLotDetailDto>>(lots);

        // Optionally fill images/pricing per lot from context by ProductId (if needed for display)
        var productIds = lots.Select(pl => pl.ProductId).Distinct().ToList();
        var imagesByProduct = await _context.ProductImages
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();
        var pricingsByProduct = await _context.Pricings
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync();
        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var pricingLookup = pricingsByProduct.ToDictionary(p => p.ProductId);
        foreach (var dto in lotDtos)
        {
            if (imageLookup.TryGetValue(dto.ProductId, out var images) && images.Any())
            {
                dto.MainImageUrl = images.First().ImageUrl;
                dto.TotalImages = images.Count;
                dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
            }
            if (pricingLookup.TryGetValue(dto.ProductId, out var pricing))
            {
                dto.OriginalUnitPrice = pricing.OriginalUnitPrice;
                dto.SuggestedUnitPrice = pricing.SuggestedUnitPrice;
                dto.FinalUnitPrice = pricing.FinalUnitPrice;
            }
        }

        // Calculate expiry status for each lot
        foreach (var dto in lotDtos)
        {
            var timeRemaining = dto.ExpiryDate - now;
            dto.DaysRemaining = (int)Math.Floor(timeRemaining.TotalDays);

            if (timeRemaining.TotalDays < 0)
            {
                dto.ExpiryStatus = ExpiryStatus.Expired;
                dto.ExpiryStatusText = $"Quá hạn {Math.Abs(dto.DaysRemaining)} ngày";
            }
            else if (timeRemaining.TotalDays < 1)
            {
                dto.ExpiryStatus = ExpiryStatus.Today;
                dto.HoursRemaining = (int)Math.Floor(timeRemaining.TotalHours);
                dto.ExpiryStatusText = dto.HoursRemaining > 0
                    ? $"Còn {dto.HoursRemaining} giờ"
                    : "Hết hạn trong vài phút";
            }
            else if (timeRemaining.TotalDays <= 2)
            {
                dto.ExpiryStatus = ExpiryStatus.ExpiringSoon;
                dto.ExpiryStatusText = $"Sắp hết hạn ({dto.DaysRemaining} ngày)";
            }
            else if (timeRemaining.TotalDays <= 7)
            {
                dto.ExpiryStatus = ExpiryStatus.ShortTerm;
                dto.ExpiryStatusText = $"Còn ngắn hạn ({dto.DaysRemaining} ngày)";
            }
            else
            {
                dto.ExpiryStatus = ExpiryStatus.LongTerm;
                dto.ExpiryStatusText = $"Còn dài hạn ({dto.DaysRemaining} ngày)";
            }
        }

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
            .Include(p => p.ProductDetail)
            .Include(p => p.Unit)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .Where(p => p.SupermarketId == supermarketId)
            .AsQueryable();

        // Search theo tên, brand hoặc barcode
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                (p.ProductDetail != null && p.ProductDetail.Brand != null && p.ProductDetail.Brand.ToLower().Contains(searchLower)) ||
                p.Barcode.Contains(searchLower)
            );
        }

        // Filter theo category (from CategoryRef.Name)
        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.CategoryRef != null && p.CategoryRef.Name == category);
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();
        var imagesByProduct = await _context.ProductImages
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();
        var pricingsByProduct = await _context.Pricings
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync();
        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var pricingLookup = pricingsByProduct.ToDictionary(p => p.ProductId);

        var productDtos = new List<ProductResponseDto>();
        foreach (var product in products)
        {
            var dto = _mapper.Map<ProductResponseDto>(product);
            var images = imageLookup.GetValueOrDefault(product.ProductId) ?? new List<ProductImage>();
            dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
            dto.TotalImages = images.Count;
            dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
            if (pricingLookup.TryGetValue(product.ProductId, out var pricing))
            {
                dto.OriginalPrice = pricing.OriginalUnitPrice;
                dto.SuggestedPrice = pricing.SuggestedUnitPrice;
                dto.FinalPrice = pricing.FinalUnitPrice;
                dto.PricingConfidence = pricing.PricingConfidence;
                dto.PricedBy = pricing.PricedBy;
                dto.PricedAt = pricing.PricedAt;
            }
            productDtos.Add(dto);
        }

        return (productDtos, totalCount);
    }

    /// <summary>
    /// Lấy thông tin chi tiết đầy đủ của sản phẩm (như nhãn sản phẩm)
    /// </summary>
    public async Task<ProductDetailDto?> GetProductDetailAsync(Guid productId)
    {
        var product = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.ProductLots)
            .Include(p => p.Unit)
            .Include(p => p.Supermarket)
            .Include(p => p.CategoryRef)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return null;

        var images = await _context.ProductImages
            .Where(x => x.ProductId == productId)
            .OrderBy(i => i.UploadedAt)
            .ToListAsync();
        var pricing = await _context.Pricings
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        var detail = _mapper.Map<ProductDetailDto>(product);
        detail.UnitName = product.Unit?.Name ?? "Đang cập nhật";
        detail.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        detail.TotalImages = images.Count;
        detail.ProductImages = _mapper.Map<List<ProductImageDto>>(images);

        if (pricing != null && pricing.OriginalUnitPrice > 0 && pricing.FinalUnitPrice > 0)
        {
            detail.DiscountPercent = Math.Round((1 - pricing.FinalUnitPrice / pricing.OriginalUnitPrice) * 100, 1);
        }

        // Tính số lượng tồn kho từ các lots
        detail.Quantity = await _context.ProductLots
            .Where(pl => pl.ProductId == productId)
            .SumAsync(pl => pl.Quantity);

        // Lấy ExpiryDate từ lot gần hết hạn nhất
        var nearestLot = await _context.ProductLots
            .Where(pl => pl.ProductId == productId && pl.ExpiryDate >= DateTime.UtcNow)
            .OrderBy(pl => pl.ExpiryDate)
            .FirstOrDefaultAsync();

        if (nearestLot != null)
        {
            var today = DateTime.UtcNow.Date;
            detail.DaysToExpiry = (nearestLot.ExpiryDate.Date - today).Days;

            if (detail.DaysToExpiry < 0)
            {
                detail.ExpiryStatus = ExpiryStatus.Expired;
                detail.ExpiryStatusText = $"Quá hạn {Math.Abs(detail.DaysToExpiry.Value)} ngày";
            }
            else if (detail.DaysToExpiry == 0)
            {
                detail.ExpiryStatus = ExpiryStatus.Today;
                detail.ExpiryStatusText = "Hết hạn trong ngày";
            }
            else if (detail.DaysToExpiry <= 2)
            {
                detail.ExpiryStatus = ExpiryStatus.ExpiringSoon;
                detail.ExpiryStatusText = $"Sắp hết hạn (còn {detail.DaysToExpiry} ngày)";
            }
            else if (detail.DaysToExpiry <= 7)
            {
                detail.ExpiryStatus = ExpiryStatus.ShortTerm;
                detail.ExpiryStatusText = $"Còn {detail.DaysToExpiry} ngày";
            }
            else
            {
                detail.ExpiryStatus = ExpiryStatus.LongTerm;
                detail.ExpiryStatusText = $"Còn {detail.DaysToExpiry} ngày";
            }
        }

        return detail;
    }
}

