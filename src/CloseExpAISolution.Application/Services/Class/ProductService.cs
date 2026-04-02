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

    public async Task<ProductResponseDto?> GetByIdWithImagesAsync(Guid id, bool includeHiddenDeletedProducts = false)
    {
        var product = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null) return null;

        if (!includeHiddenDeletedProducts &&
            (product.Status == ProductState.Hidden || product.Status == ProductState.Deleted))
            return null;

        var dto = _mapper.Map<ProductResponseDto>(product);
        var images = await _context.ProductImages
            .Where(x => x.ProductId == id)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken: default);
        var pricing = await GetLatestPricingHistoryByProductIdAsync(id, default);

        dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        dto.TotalImages = images.Count;
        dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
        if (pricing != null)
        {
            dto.SuggestedPrice = pricing.SuggestedPrice;
            dto.PricingConfidence = (float)pricing.AIConfidence;
            dto.PricedBy = pricing.ConfirmedBy;
            dto.PricedAt = pricing.ConfirmedAt;
        }
        return dto;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllWithImagesAsync(bool includeHiddenDeletedProducts = false)
    {
        var query = _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .AsQueryable();

        if (!includeHiddenDeletedProducts)
        {
            query = query.Where(p => p.Status != ProductState.Hidden && p.Status != ProductState.Deleted);
        }

        var products = await query.ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();
        var imagesByProduct = await _context.ProductImages
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var pricingLookup = await BuildLatestPricingHistoryLookupAsync(productIds, default);

        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());

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
                dto.SuggestedPrice = pricing.SuggestedPrice;
                dto.PricingConfidence = (float)pricing.AIConfidence;
                dto.PricedBy = pricing.ConfirmedBy;
                dto.PricedAt = pricing.ConfirmedAt;
            }
            result.Add(dto);
        }
        return result;
    }

    public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = _mapper.Map<Product>(request);

        var defaultUnit = await _context.UnitOfMeasures.FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var category = await _context.Categories.FirstOrDefaultAsync(
                c => c.Name != null && c.Name.ToLower() == request.CategoryName.ToLower(),
                cancellationToken);
            if (category != null)
            {
                product.CategoryId = category.CategoryId;
            }
        }

        var added = await _unitOfWork.ProductRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var detail = new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = added.ProductId,
            Brand = request.Detail.Brand,
            Ingredients = request.Detail.Ingredients,
            NutritionFacts = request.Detail.NutritionFactsJson,
            UsageInstructions = request.Detail.UsageInstructions,
            StorageInstructions = request.Detail.StorageInstructions,
            Manufacturer = request.Detail.Manufacturer,
            Origin = request.Detail.Origin,
            Description = request.Detail.Description,
            SafetyWarning = request.Detail.SafetyWarnings
        };
        await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var withDetail = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .FirstOrDefaultAsync(p => p.ProductId == added.ProductId, cancellationToken);
        if (withDetail == null) throw new InvalidOperationException("Product not found after create.");

        var dto = _mapper.Map<ProductResponseDto>(withDetail);
        var images = await _context.ProductImages.Where(x => x.ProductId == added.ProductId).OrderBy(i => i.CreatedAt).ToListAsync(cancellationToken);
        var pricing = await GetLatestPricingHistoryByProductIdAsync(added.ProductId, cancellationToken);
        dto.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        dto.TotalImages = images.Count;
        dto.ProductImages = _mapper.Map<List<ProductImageDto>>(images);
        if (pricing != null)
        {
            dto.SuggestedPrice = pricing.SuggestedPrice;
            dto.PricingConfidence = (float)pricing.AIConfidence;
            dto.PricedBy = pricing.ConfirmedBy;
            dto.PricedAt = pricing.ConfirmedAt;
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

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var category = await _context.Categories.FirstOrDefaultAsync(
                c => c.Name != null && c.Name.ToLower() == request.CategoryName.ToLower(),
                cancellationToken);
            if (category != null)
            {
                product.CategoryId = category.CategoryId;
            }
        }

        var detail = product.ProductDetail ?? new ProductDetail { ProductDetailId = Guid.NewGuid(), ProductId = product.ProductId };
        detail.Brand = request.Detail.Brand;
        detail.Ingredients = request.Detail.Ingredients;
        detail.NutritionFacts = request.Detail.NutritionFactsJson;
        detail.UsageInstructions = request.Detail.UsageInstructions;
        detail.StorageInstructions = request.Detail.StorageInstructions;
        detail.Manufacturer = request.Detail.Manufacturer;
        detail.Origin = request.Detail.Origin;
        detail.Description = request.Detail.Description;
        detail.SafetyWarning = request.Detail.SafetyWarnings;
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

    public async Task<(IEnumerable<StockLotDetailDto> Items, int TotalCount)> GetStockLotsBySupermarketAsync(
        StockLotFilterDto filter,
        bool includeHiddenDeletedProducts = false)
    {
        var query = _context.StockLots
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.Supermarket)
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.ProductDetail)
            .Include(pl => pl.Product)
                .ThenInclude(p => p!.CategoryRef)
            .AsQueryable();

        if (filter.SupermarketId.HasValue)
        {
            query = query.Where(pl => pl.Product!.SupermarketId == filter.SupermarketId.Value);
        }

        if (!includeHiddenDeletedProducts)
        {
            query = query.Where(pl =>
                pl.Product != null
                && pl.Product.Status != ProductState.Hidden
                && pl.Product.Status != ProductState.Deleted);
        }

        if (filter.IsFreshFood.HasValue)
        {
            query = query.Where(pl => pl.Product!.CategoryRef != null && pl.Product.CategoryRef.IsFreshFood == filter.IsFreshFood.Value);
        }

        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(pl => pl.Product!.CategoryRef != null && pl.Product.CategoryRef.Name == filter.Category);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchLower = filter.SearchTerm.ToLower();
            query = query.Where(pl =>
                pl.Product!.Name.ToLower().Contains(searchLower) ||
                (pl.Product!.ProductDetail != null && pl.Product.ProductDetail.Brand != null && pl.Product.ProductDetail.Brand.ToLower().Contains(searchLower)) ||
                pl.Product!.Barcode.Contains(searchLower)
            );
        }

        var lots = await query.ToListAsync();
        var now = DateTime.UtcNow;

        var lotDtos = _mapper.Map<List<StockLotDetailDto>>(lots);

        var productIds = lots.Select(pl => pl.ProductId).Distinct().ToList();
        var imagesByProduct = await _context.ProductImages
            .Where(x => productIds.Contains(x.ProductId))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var pricingLookup = await BuildLatestPricingHistoryLookupAsync(productIds, default);
        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());
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
                if (pricing.SuggestedPrice > 0)
                {
                    dto.SuggestedUnitPrice = pricing.SuggestedPrice;
                }
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

        if (filter.ExpiryStatus.HasValue)
        {
            lotDtos = lotDtos.Where(dto => dto.ExpiryStatus == filter.ExpiryStatus.Value).ToList();
        }

        // Priority: Today → ExpiringSoon → ShortTerm → LongTerm → Expired (cuối cùng)
        lotDtos = lotDtos
            .OrderBy(dto => dto.ExpiryStatus == ExpiryStatus.Expired ? 99 : (int)dto.ExpiryStatus)
            .ThenBy(dto => dto.ExpiryDate)
            .ToList();

        var totalCount = lotDtos.Count;

        var pagedLots = lotDtos
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return (pagedLots, totalCount);
    }

    public async Task<(IEnumerable<AvailableStocklotDto> Items, int TotalCount)> GetAvailableStockLotsForCustomerAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var now = DateTime.UtcNow;

        var baseQuery = _context.StockLots
            .AsNoTracking()
            .Where(l =>
                l.Product != null
                && l.Product.Status != ProductState.Hidden
                && l.Product.Status != ProductState.Deleted
                && l.Status == ProductState.Published &&
                l.Quantity > 0 &&
                l.ExpiryDate > now);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderBy(l => l.ExpiryDate)
            .Select(l => new AvailableStocklotDto
            {
                LotId = l.LotId,
                ProductId = l.ProductId,
                ProductName = l.Product != null ? l.Product.Name : string.Empty,
                ProductImageUrl = l.Product != null
                    ? l.Product.ProductImages
                        .OrderByDescending(pi => pi.IsPrimary)
                        .ThenBy(pi => pi.CreatedAt)
                        .Select(pi => pi.ImageUrl)
                        .FirstOrDefault()
                    : null,
                Barcode = l.Product != null ? l.Product.Barcode : string.Empty,
                Brand = l.Product != null && l.Product.ProductDetail != null
                    ? (l.Product.ProductDetail.Brand ?? string.Empty)
                    : string.Empty,
                SupermarketId = l.Product != null ? l.Product.SupermarketId : Guid.Empty,
                SupermarketName = l.Product != null && l.Product.Supermarket != null
                    ? l.Product.Supermarket.Name
                    : string.Empty,
                UnitId = l.UnitId,
                UnitName = l.Unit != null ? l.Unit.Name : string.Empty,
                Quantity = l.Quantity,
                Weight = l.Weight,
                Status = l.Status.ToString(),
                ManufactureDate = l.ManufactureDate,
                ExpiryDate = l.ExpiryDate,
                CreatedAt = l.CreatedAt,
                PublishedBy = l.PublishedBy,
                PublishedAt = l.PublishedAt,
                OriginalUnitPrice = l.OriginalUnitPrice,
                SuggestedUnitPrice = l.SuggestedUnitPrice,
                FinalUnitPrice = l.FinalUnitPrice,
                SellingUnitPrice = l.FinalUnitPrice ?? l.SuggestedUnitPrice
            })
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var remainingDays = (item.ExpiryDate.Date - now.Date).Days;
            item.DaysRemaining = remainingDays < 0 ? 0 : remainingDays;
        }

        return (items, totalCount);
    }

    public async Task<(IEnumerable<ProductResponseDto> Items, int TotalCount)> GetProductsBySupermarketAsync(
        Guid supermarketId,
        string? searchTerm = null,
        string? category = null,
        int pageNumber = 1,
        int pageSize = 20,
        bool includeHiddenDeletedProducts = false)
    {
        var query = _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.CategoryRef)
            .Include(p => p.Supermarket)
            .Where(p => p.SupermarketId == supermarketId)
            .AsQueryable();

        if (!includeHiddenDeletedProducts)
        {
            query = query.Where(p => p.Status != ProductState.Hidden && p.Status != ProductState.Deleted);
        }

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                (p.ProductDetail != null && p.ProductDetail.Brand != null && p.ProductDetail.Brand.ToLower().Contains(searchLower)) ||
                p.Barcode.Contains(searchLower)
            );
        }

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
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var pricingLookup = await BuildLatestPricingHistoryLookupAsync(productIds, default);
        var imageLookup = imagesByProduct.GroupBy(x => x.ProductId).ToDictionary(g => g.Key, g => g.ToList());

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
                dto.SuggestedPrice = pricing.SuggestedPrice;
                dto.PricingConfidence = (float)pricing.AIConfidence;
                dto.PricedBy = pricing.ConfirmedBy;
                dto.PricedAt = pricing.ConfirmedAt;
            }
            productDtos.Add(dto);
        }

        return (productDtos, totalCount);
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(Guid productId, bool includeHiddenDeletedProducts = false)
    {
        var product = await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.Supermarket)
            .Include(p => p.CategoryRef)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return null;

        if (!includeHiddenDeletedProducts &&
            (product.Status == ProductState.Hidden || product.Status == ProductState.Deleted))
            return null;

        var images = await _context.ProductImages
            .Where(x => x.ProductId == productId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var pricing = await GetLatestPricingHistoryByProductIdAsync(productId, default);

        var detail = _mapper.Map<ProductDetailDto>(product);
        detail.UnitName = "Đang cập nhật";
        detail.MainImageUrl = images.Any() ? images.First().ImageUrl : null;
        detail.TotalImages = images.Count;
        detail.ProductImages = _mapper.Map<List<ProductImageDto>>(images);

        if (pricing != null && pricing.SuggestedPrice > 0)
        {
            detail.SuggestedPrice = pricing.SuggestedPrice;
        }

        detail.Quantity = await _context.StockLots
            .Where(pl => pl.ProductId == productId)
            .SumAsync(pl => pl.Quantity);

        var nearestLot = await _context.StockLots
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

    private async Task<PricingHistory?> GetLatestPricingHistoryByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var lotIds = await _context.StockLots
            .Where(l => l.ProductId == productId)
            .Select(l => l.LotId)
            .ToListAsync(cancellationToken);

        if (!lotIds.Any())
            return null;

        return await _context.PricingHistories
            .Where(h => lotIds.Contains(h.LotId))
            .OrderByDescending(h => h.ConfirmedAt ?? h.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, PricingHistory>> BuildLatestPricingHistoryLookupAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        var idList = productIds.Distinct().ToList();
        if (!idList.Any())
            return new Dictionary<Guid, PricingHistory>();

        var lots = await _context.StockLots
            .Where(l => idList.Contains(l.ProductId))
            .Select(l => new { l.ProductId, l.LotId })
            .ToListAsync(cancellationToken);

        var lotIds = lots.Select(x => x.LotId).Distinct().ToList();
        if (!lotIds.Any())
            return new Dictionary<Guid, PricingHistory>();

        var histories = await _context.PricingHistories
            .Where(h => lotIds.Contains(h.LotId))
            .ToListAsync(cancellationToken);

        var lotToProduct = lots.ToDictionary(x => x.LotId, x => x.ProductId);

        return histories
            .Where(h => lotToProduct.ContainsKey(h.LotId))
            .GroupBy(h => lotToProduct[h.LotId])
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(h => h.ConfirmedAt ?? h.CreatedAt).First());
    }
}


