using System.Text.Json;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductWorkflowService : IProductWorkflowService
{
    private const int WorkflowAiTimeoutSeconds = 30;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIServiceClient _aiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMarketPriceService _marketPriceService;
    private readonly IBarcodeLookupService _barcodeLookupService;
    private readonly ILogger<ProductWorkflowService> _logger;

    public ProductWorkflowService(
        IUnitOfWork unitOfWork,
        IAIServiceClient aiClient,
        IServiceProvider serviceProvider,
        IMarketPriceService marketPriceService,
        IBarcodeLookupService barcodeLookupService,
        ILogger<ProductWorkflowService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiClient = aiClient;
        _serviceProvider = serviceProvider;
        _marketPriceService = marketPriceService;
        _barcodeLookupService = barcodeLookupService;
        _logger = logger;
    }

    public async Task<ProductResponseDto> VerifyProductAsync(
        Guid productId,
        VerifyProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        if (product.Status != ProductState.Draft)
        {
            throw new InvalidOperationException($"Product must be in Draft status to verify. Current status: {product.Status}");
        }

        _logger.LogInformation("Verifying product {ProductId}", productId);

        if (!string.IsNullOrEmpty(request.Name))
            product.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Barcode))
            product.Barcode = request.Barcode;

        var detail = product.ProductDetail ?? new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = product.ProductId
        };

        if (!string.IsNullOrEmpty(request.Detail?.Brand))
            detail.Brand = request.Detail.Brand;

        if (request.Detail != null)
        {
            if (!string.IsNullOrEmpty(request.Detail.Ingredients)) detail.Ingredients = SerializeIngredientsForStorage(ParseIngredients(request.Detail.Ingredients));
            if (!string.IsNullOrEmpty(request.Detail.NutritionFactsJson)) detail.NutritionFacts = request.Detail.NutritionFactsJson;
            if (!string.IsNullOrEmpty(request.Detail.UsageInstructions)) detail.UsageInstructions = request.Detail.UsageInstructions;
            if (!string.IsNullOrEmpty(request.Detail.StorageInstructions)) detail.StorageInstructions = request.Detail.StorageInstructions;
            if (!string.IsNullOrEmpty(request.Detail.Manufacturer)) detail.Manufacturer = request.Detail.Manufacturer;
            if (!string.IsNullOrEmpty(request.Detail.Origin)) detail.Origin = request.Detail.Origin;
            if (!string.IsNullOrEmpty(request.Detail.Description)) detail.Description = request.Detail.Description;
            if (!string.IsNullOrEmpty(request.Detail.SafetyWarnings)) detail.SafetyWarning = request.Detail.SafetyWarnings;
        }

        if (!string.IsNullOrEmpty(request.CategoryName))
        {
            var category = await ResolveCategoryByNameAsync(request.CategoryName, cancellationToken);
            if (category != null)
            {
                product.CategoryId = category.CategoryId;
            }
        }

        if (product.ProductDetail == null)
        {
            await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
            product.ProductDetail = detail;
        }
        else
        {
            _unitOfWork.Repository<ProductDetail>().Update(detail);
        }

        // Update StockLot dates if provided
        var lot = await GetLatestStockLotByProductIdAsync(product.ProductId);
        if (lot != null)
        {
            if (request.ExpiryDate.HasValue) lot.ExpiryDate = EnsureUtc(request.ExpiryDate.Value);
            if (request.ManufactureDate.HasValue) lot.ManufactureDate = EnsureUtc(request.ManufactureDate.Value);

            if (!await HasRequiredShelfLifeAsync(lot.ExpiryDate, cancellationToken))
            {
                var minHours = await GetMinimumShelfLifeHoursAsync(cancellationToken);
                throw new InvalidOperationException($"Product lot must have remaining shelf life > {minHours} hours to continue workflow.");
            }

            _unitOfWork.Repository<StockLot>().Update(lot);
        }

        product.VerifiedBy = request.VerifiedBy;
        product.VerifiedAt = DateTime.UtcNow;
        product.Status = ProductState.Verified;

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToResponseDtoAsync(product);
    }

    private async Task<PricingSuggestionResponseDto> GetPricingSuggestionInternalAsync(
        Product product,
        decimal originalPrice,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pricing suggestion for product {ProductId}", product.ProductId);

        List<MarketPriceSourceDto> marketSources = new();
        decimal? minMarketPrice = null;
        decimal? avgMarketPrice = null;
        decimal? maxMarketPrice = null;

        if (!string.IsNullOrEmpty(product.Barcode) || !string.IsNullOrEmpty(product.Name))
        {
            try
            {
                var marketPriceResult = await _marketPriceService.GetMarketPriceAsync(
                    product.Barcode ?? "",
                    cancellationToken);

                if (marketPriceResult == null || !marketPriceResult.Details.Any())
                {
                    _logger.LogInformation("No market prices in DB, triggering crawl for {ProductName}", product.Name);

                    var crawlResult = await _marketPriceService.TriggerCrawlAsync(
                        product.Barcode ?? "",
                        product.Name,
                        cancellationToken);

                    if (crawlResult.Success && crawlResult.PricesFound > 0)
                    {
                        _logger.LogInformation("Crawl succeeded with {Count} prices, fetching from DB...", crawlResult.PricesFound);

                        marketPriceResult = await _marketPriceService.GetMarketPriceAsync(
                            product.Barcode ?? "",
                            cancellationToken);

                        if ((marketPriceResult == null || !marketPriceResult.Details.Any())
                            && !string.IsNullOrEmpty(product.Name))
                        {
                            marketPriceResult = await _marketPriceService.SearchMarketPriceAsync(
                                product.Name,
                                cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Crawl failed or returned no prices: {Error}", crawlResult.Error);
                    }
                }

                if (marketPriceResult != null && marketPriceResult.Details.Any())
                {
                    minMarketPrice = marketPriceResult.MinPrice;
                    avgMarketPrice = marketPriceResult.AvgPrice;
                    maxMarketPrice = marketPriceResult.MaxPrice;

                    marketSources = marketPriceResult.Details.Select(p => new MarketPriceSourceDto
                    {
                        StoreName = p.StoreName ?? p.Source,
                        Price = p.Price,
                        Source = p.Source
                    }).ToList();

                    _logger.LogInformation("Using {Count} market prices for comparison", marketPriceResult.Details.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting market prices for product {ProductName}", product.Name);
            }
        }

        // Get expiry date from StockLot
        var lot = await GetLatestStockLotByProductIdAsync(product.ProductId);
        var expiryDate = lot?.ExpiryDate;

        int? daysToExpiry = null;
        if (expiryDate.HasValue)
        {
            daysToExpiry = (int)(expiryDate.Value - DateTime.UtcNow).TotalDays;
        }

        try
        {
            var productType = MapCategoryToProductType(product.CategoryRef?.Name);

            DateTime? expiryDateOnly = expiryDate.HasValue
                ? expiryDate.Value.Date
                : null;

            var pricingRequest = new CloseExpAISolution.Application.AIService.Models.PricingRequest
            {
                ProductType = productType,
                DaysToExpire = daysToExpiry ?? 30,
                BasePrice = originalPrice,
                ExpiryDate = expiryDateOnly,
                Brand = product.ProductDetail?.Brand,
                MinMarketPrice = minMarketPrice,
                AvgMarketPrice = avgMarketPrice,
                ProductName = product.Name,
                Barcode = product.Barcode
            };

            var latestVerificationLog = await GetLatestVerificationLogByProductIdAsync(product.ProductId);
            var (freshnessLevel, freshnessScore) = ExtractFreshnessFromRawData(latestVerificationLog?.RawData);
            pricingRequest.FreshnessLevel = freshnessLevel;
            pricingRequest.FreshnessScore = freshnessScore;

            var pricingResult = await _aiClient.GetPriceSuggestionAsync(pricingRequest, cancellationToken);

            if (pricingResult != null)
            {
                var discountPercent = originalPrice > 0
                    ? (1 - pricingResult.SuggestedPrice / originalPrice) * 100
                    : 0;

                return new PricingSuggestionResponseDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    OriginalPrice = originalPrice,
                    SuggestedPrice = pricingResult.SuggestedPrice,
                    Confidence = pricingResult.Confidence,
                    DiscountPercent = Math.Round(discountPercent, 1),
                    ExpiryDate = expiryDate,
                    DaysToExpiry = daysToExpiry,
                    Reasons = pricingResult.Reasons?.ToList() ?? new List<string>(),
                    MinMarketPrice = minMarketPrice,
                    AvgMarketPrice = avgMarketPrice,
                    MaxMarketPrice = maxMarketPrice,
                    MarketPriceSources = marketSources
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI pricing for product {ProductId}", product.ProductId);
        }

        var fallbackDiscount = CalculateFallbackDiscount(daysToExpiry);
        var fallbackPrice = originalPrice * (1 - fallbackDiscount / 100);

        return new PricingSuggestionResponseDto
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            OriginalPrice = originalPrice,
            SuggestedPrice = fallbackPrice,
            Confidence = 0.5f,
            DiscountPercent = fallbackDiscount,
            ExpiryDate = expiryDate,
            DaysToExpiry = daysToExpiry,
            Reasons = new List<string> { "Fallback calculation based on days to expiry" },
            MinMarketPrice = minMarketPrice,
            AvgMarketPrice = avgMarketPrice,
            MaxMarketPrice = maxMarketPrice,
            MarketPriceSources = marketSources
        };
    }

    private decimal CalculateFallbackDiscount(int? daysToExpiry)
    {
        if (!daysToExpiry.HasValue) return 10;

        return daysToExpiry.Value switch
        {
            <= 0 => 80,
            <= 3 => 60,
            <= 7 => 40,
            <= 14 => 25,
            <= 30 => 15,
            _ => 10
        };
    }

    public async Task<ProductResponseDto?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(productId);
        return product == null ? null : await MapToResponseDtoAsync(product);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetProductsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.ProductRepository.FindAsync(
            p => p.SupermarketId == supermarketId && p.Status == status);

        var responses = await Task.WhenAll(products.Select(p => MapToResponseDtoAsync(p)));
        return responses;
    }

    public async Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.ProductRepository.FindAsync(
            p => p.SupermarketId == supermarketId);

        var productList = products.ToList();

        return new WorkflowSummaryDto
        {
            DraftCount = productList.Count(p => p.Status == ProductState.Draft),
            VerifiedCount = productList.Count(p => p.Status == ProductState.Verified),
            PricedCount = productList.Count(p => p.Status == ProductState.Priced),
            PublishedCount = productList.Count(p => p.Status == ProductState.Published),
            ExpiredCount = productList.Count(p => p.Status == ProductState.Expired),
            TotalCount = productList.Count
        };
    }

    private async Task<ProductResponseDto> MapToResponseDtoAsync(Product product, BarcodeProductInfo? barcodeLookupInfo = null)
    {
        var lot = await GetLatestStockLotByProductIdAsync(product.ProductId);
        var pricing = await GetLatestPricingHistoryByLotIdAsync(lot?.LotId);
        var expiryDate = lot != null ? (DateTime?)lot.ExpiryDate : null;
        var manufactureDate = lot != null ? (DateTime?)lot.ManufactureDate : null;

        int? daysToExpiry = null;
        if (expiryDate.HasValue)
        {
            daysToExpiry = (int)(expiryDate.Value - DateTime.UtcNow).TotalDays;
        }

        var response = new ProductResponseDto
        {
            ProductId = product.ProductId,
            SupermarketId = product.SupermarketId,
            Name = product.Name,
            Brand = product.ProductDetail?.Brand ?? string.Empty,
            Category = product.CategoryRef?.Name ?? string.Empty,
            Barcode = product.Barcode,
            IsFreshFood = product.CategoryRef?.IsFreshFood ?? false,
            Status = product.Status,
            OriginalPrice = lot?.OriginalUnitPrice ?? 0,
            SuggestedPrice = pricing?.SuggestedPrice ?? 0,
            FinalPrice = 0,
            ExpiryDate = expiryDate,
            ManufactureDate = manufactureDate,
            DaysToExpiry = daysToExpiry,
            PricingConfidence = pricing != null ? (float)pricing.AIConfidence : 0f,
            PricingReasons = pricing?.Reason,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            VerifiedBy = product.VerifiedBy,
            VerifiedAt = product.VerifiedAt,
            PricedBy = pricing?.ConfirmedBy,
            PricedAt = pricing?.ConfirmedAt
        };

        if (barcodeLookupInfo != null)
        {
            response.BarcodeLookupInfo = new BarcodeLookupInfoDto
            {
                Barcode = barcodeLookupInfo.Barcode,
                ProductName = barcodeLookupInfo.ProductName,
                Brand = barcodeLookupInfo.Brand,
                Category = barcodeLookupInfo.Category,
                Description = barcodeLookupInfo.Description,
                ImageUrl = barcodeLookupInfo.ImageUrl,
                Manufacturer = barcodeLookupInfo.Manufacturer,
                Weight = barcodeLookupInfo.Weight,
                Ingredients = ParseIngredients(barcodeLookupInfo.Ingredients),
                NutritionFacts = barcodeLookupInfo.NutritionFacts,
                Country = barcodeLookupInfo.Country,
                Source = barcodeLookupInfo.Source,
                Confidence = barcodeLookupInfo.Confidence,
                IsVietnameseProduct = barcodeLookupInfo.IsVietnameseProduct,
                Gs1Prefix = barcodeLookupInfo.Gs1Prefix,
                ScanCount = barcodeLookupInfo.ScanCount,
                IsVerified = barcodeLookupInfo.IsVerified
            };
        }

        return response;
    }

    private static decimal? ParseWeightToDecimal(string? weightStr)
    {
        if (string.IsNullOrWhiteSpace(weightStr)) return null;
        var s = weightStr.Trim().ToLowerInvariant();
        var numStr = new string(s.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        if (string.IsNullOrEmpty(numStr) || !decimal.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var value))
            return null;
        if (s.Contains("kg") || s.Contains("kilo")) return value;
        if (s.Contains("g") || s.Contains("gram")) return value / 1000m;
        if (s.Contains("l") || s.Contains("lit")) return value;
        if (s.Contains("ml")) return value / 1000m;
        return value; // assume kg if no unit
    }

    private bool IsFreshFoodCategory(string? category)
    {
        if (string.IsNullOrEmpty(category)) return false;

        var freshCategories = new[]
        {
            "meat", "seafood", "produce", "dairy", "bakery",
            "thịt", "hải sản", "rau củ", "sữa", "bánh"
        };

        return freshCategories.Any(c =>
            category.Contains(c, StringComparison.OrdinalIgnoreCase));
    }

    private string MapCategoryToProductType(string? category)
    {
        if (string.IsNullOrEmpty(category))
            return "other";

        var lowerCategory = category.ToLowerInvariant();

        // Direct matches
        var validTypes = new[] { "dairy", "meat", "seafood", "bakery", "produce", "frozen", "beverage", "snack", "condiment" };
        foreach (var type in validTypes)
        {
            if (lowerCategory.Contains(type))
                return type;
        }

        var categoryMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "sữa", "dairy" },
            { "sua", "dairy" },
            { "phô mai", "dairy" },
            { "pho mai", "dairy" },
            { "sữa chua", "dairy" },
            { "bơ", "dairy" },

            { "thịt", "meat" },
            { "thit", "meat" },
            { "xúc xích", "meat" },
            { "xuc xich", "meat" },
            { "giò", "meat" },
            { "chả", "meat" },
            { "cha", "meat" },
            { "jambon", "meat" },
            { "ham", "meat" },
            { "bacon", "meat" },
            { "lạp xưởng", "meat" },

            { "hải sản", "seafood" },
            { "hai san", "seafood" },
            { "cá", "seafood" },
            { "ca", "seafood" },
            { "tôm", "seafood" },
            { "tom", "seafood" },
            { "mực", "seafood" },
            { "cua", "seafood" },

            { "bánh", "bakery" },
            { "banh", "bakery" },
            { "bread", "bakery" },

            { "rau", "produce" },
            { "củ", "produce" },
            { "cu", "produce" },
            { "quả", "produce" },
            { "qua", "produce" },
            { "trái cây", "produce" },
            { "trai cay", "produce" },
            { "vegetable", "produce" },
            { "fruit", "produce" },

            { "đông lạnh", "frozen" },
            { "dong lanh", "frozen" },
            { "kem", "frozen" },

            { "nước", "beverage" },
            { "nuoc", "beverage" },
            { "đồ uống", "beverage" },
            { "do uong", "beverage" },
            { "trà", "beverage" },
            { "tra", "beverage" },
            { "cà phê", "beverage" },
            { "ca phe", "beverage" },
            { "coffee", "beverage" },
            { "tea", "beverage" },
            { "juice", "beverage" },
            { "nước ngọt", "beverage" },

            { "snack", "snack" },
            { "bánh snack", "snack" },
            { "chip", "snack" },
            { "kẹo", "snack" },
            { "keo", "snack" },
            { "chocolate", "snack" },
            { "ăn vặt", "snack" },
            { "an vat", "snack" },

            { "gia vị", "condiment" },
            { "gia vi", "condiment" },
            { "nước mắm", "condiment" },
            { "nuoc mam", "condiment" },
            { "nước tương", "condiment" },
            { "nuoc tuong", "condiment" },
            { "sauce", "condiment" },
            { "sốt", "condiment" },
            { "dầu ăn", "condiment" },
            { "dau an", "condiment" },
            { "muối", "condiment" },
            { "đường", "condiment" },
            { "bột", "condiment" },

            { "đồ hộp", "other" },
            { "do hop", "other" },
            { "thực phẩm đóng hộp", "meat" }, // Usually canned meat
            { "thuc pham dong hop", "meat" }
        };

        foreach (var mapping in categoryMappings)
        {
            if (lowerCategory.Contains(mapping.Key))
                return mapping.Value;
        }

        return "other";
    }

    public async Task<ScanBarcodeResponseDto> ScanBarcodeAsync(
        string barcode,
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scanning barcode {Barcode} for supermarket {SupermarketId}", barcode, supermarketId);

        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);
        if (supermarket == null)
        {
            throw new ArgumentException($"Supermarket with ID {supermarketId} not found.", nameof(supermarketId));
        }

        var existingProducts = (await _unitOfWork.ProductRepository.FindAsync(
            p => p.Barcode == barcode && p.Status != ProductState.Hidden && p.Status != ProductState.Deleted))
            .ToList();

        if (existingProducts.Count > 0)
        {
            var detailed = new List<Product>(existingProducts.Count);
            foreach (var p in existingProducts)
            {
                var productWithDetails = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(p.ProductId);
                detailed.Add(productWithDetails ?? p);
            }

            var matchedProducts = detailed
                .OrderByDescending(p => p.SupermarketId == supermarketId)
                .ThenByDescending(p => p.Status == ProductState.Verified)
                .Select(p => new MatchedBarcodeProductDto
                {
                    ProductId = p.ProductId,
                    SupermarketId = p.SupermarketId,
                    SupermarketName = p.Supermarket?.Name,
                    Name = p.Name,
                    Brand = p.ProductDetail?.Brand ?? string.Empty,
                    Category = p.CategoryRef?.Name ?? string.Empty,
                    Barcode = p.Barcode,
                    Status = p.Status,
                    IsCurrentSupermarket = p.SupermarketId == supermarketId
                })
                .ToList();

            var currentMarketProduct = detailed.FirstOrDefault(p => p.SupermarketId == supermarketId);
            var selectedForDisplay = currentMarketProduct ?? detailed.First();

            var images = await _unitOfWork.Repository<ProductImage>().FindAsync(pi => pi.ProductId == selectedForDisplay.ProductId);
            var mainImage = images.OrderByDescending(i => i.CreatedAt).FirstOrDefault()?.ImageUrl;

            var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => l.ProductId == selectedForDisplay.ProductId);
            var totalLotsSold = lots.Count(l => l.Status == ProductState.Published || l.Status == ProductState.SoldOut);
            var lotIds = lots.Select(l => l.LotId).ToList();
            var pricing = await _unitOfWork.Repository<PricingHistory>().FindAsync(ph => lotIds.Contains(ph.LotId));
            var latestPricing = pricing.OrderByDescending(ph => ph.ConfirmedAt ?? ph.CreatedAt).FirstOrDefault();

            var canCreateLotDirectly = currentMarketProduct?.Status == ProductState.Verified;
            var requiresVerification = currentMarketProduct != null && currentMarketProduct.Status != ProductState.Verified;
            var canCreatePrivateProduct = currentMarketProduct == null;

            var nextAction = canCreateLotDirectly
                ? "CREATE_LOT"
                : requiresVerification
                    ? "VERIFY_PRODUCT"
                    : "CHOOSE_OR_CREATE_PRIVATE_PRODUCT";

            return new ScanBarcodeResponseDto
            {
                Barcode = barcode,
                ProductExists = true,
                ExistingProduct = new ExistingProductInfoDto
                {
                    ProductId = selectedForDisplay.ProductId,
                    Name = selectedForDisplay.Name,
                    Brand = selectedForDisplay.ProductDetail?.Brand ?? string.Empty,
                    Category = selectedForDisplay.CategoryRef?.Name ?? string.Empty,
                    Barcode = selectedForDisplay.Barcode,
                    MainImageUrl = mainImage,
                    Manufacturer = selectedForDisplay.ProductDetail?.Manufacturer,
                    Ingredients = ParseIngredients(selectedForDisplay.ProductDetail?.Ingredients),
                    LastPrice = latestPricing?.SuggestedPrice,
                    TotalLotsSold = totalLotsSold
                },
                MatchedProducts = matchedProducts,
                NextAction = nextAction,
                RequiresOcrUpload = false,
                CanCreatePrivateProductForCurrentSupermarket = canCreatePrivateProduct,
                RequiresVerification = requiresVerification,
                VerificationProductId = requiresVerification ? currentMarketProduct?.ProductId : null,
                CanCreateLotDirectly = canCreateLotDirectly
            };
        }

        _logger.LogInformation("Product not found in database for barcode {Barcode}, checking external sources", barcode);
        BarcodeProductInfo? barcodeLookupInfo = null;

        try
        {
            barcodeLookupInfo = await _barcodeLookupService.LookupAsync(barcode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error looking up barcode {Barcode}", barcode);
        }

        return new ScanBarcodeResponseDto
        {
            Barcode = barcode,
            ProductExists = false,
            BarcodeLookupInfo = barcodeLookupInfo != null ? new BarcodeLookupInfoDto
            {
                Barcode = barcodeLookupInfo.Barcode,
                ProductName = barcodeLookupInfo.ProductName,
                Brand = barcodeLookupInfo.Brand,
                Category = barcodeLookupInfo.Category,
                Description = barcodeLookupInfo.Description,
                ImageUrl = barcodeLookupInfo.ImageUrl,
                Manufacturer = barcodeLookupInfo.Manufacturer,
                Weight = barcodeLookupInfo.Weight,
                Ingredients = ParseIngredients(barcodeLookupInfo.Ingredients),
                NutritionFacts = barcodeLookupInfo.NutritionFacts,
                Country = barcodeLookupInfo.Country,
                Source = barcodeLookupInfo.Source,
                Confidence = barcodeLookupInfo.Confidence,
                IsVietnameseProduct = barcodeLookupInfo.IsVietnameseProduct,
                Gs1Prefix = barcodeLookupInfo.Gs1Prefix,
                ScanCount = barcodeLookupInfo.ScanCount,
                IsVerified = barcodeLookupInfo.IsVerified
            } : null,
            NextAction = "UPLOAD_IMAGE_FOR_OCR",
            RequiresOcrUpload = true,
            CanCreatePrivateProductForCurrentSupermarket = true,
            RequiresVerification = false,
            VerificationProductId = null,
            CanCreateLotDirectly = false
        };
    }

    public async Task<StaffProductIdentificationResponseDto> IdentifyProductForStaffAsync(
        string barcode,
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        var scanResult = await ScanBarcodeAsync(barcode, supermarketId, cancellationToken);

        return new StaffProductIdentificationResponseDto
        {
            Barcode = scanResult.Barcode,
            ProductExists = scanResult.ProductExists,
            ExistingProduct = scanResult.ExistingProduct,
            MatchedProducts = scanResult.MatchedProducts,
            BarcodeLookupInfo = scanResult.BarcodeLookupInfo,
            NextAction = scanResult.ProductExists
                ? (scanResult.CanCreateLotDirectly
                    ? "CREATE_STOCKLOT"
                    : (scanResult.RequiresVerification ? "VERIFY_PRODUCT" : "CHOOSE_OR_CREATE_PRIVATE_PRODUCT"))
                : "CREATE_PRODUCT",
            CanCreatePrivateProductForCurrentSupermarket = scanResult.CanCreatePrivateProductForCurrentSupermarket,
            RequiresVerification = scanResult.RequiresVerification,
            VerificationProductId = scanResult.VerificationProductId,
            CanCreateLotDirectly = scanResult.CanCreateLotDirectly,
            TimeoutInfo = new WorkflowTimeoutInfoDto
            {
                TimeoutSeconds = WorkflowAiTimeoutSeconds,
                IsAiStep = !scanResult.ProductExists,
                SupportsManualFallback = true
            }
        };
    }

    public async Task<CreateNewProductResponseDto> CreateProductFromStaffWorkflowAsync(
        StaffCreateProductFromWorkflowRequestDto request,
        Guid supermarketId,
        string staffName,
        CancellationToken cancellationToken = default)
    {
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);
        if (supermarket == null)
        {
            throw new ArgumentException($"Supermarket with ID {supermarketId} not found.", nameof(supermarketId));
        }

        if (string.IsNullOrWhiteSpace(request.Barcode))
        {
            throw new ArgumentException("Barcode is required.", nameof(request.Barcode));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Product name is required.", nameof(request.Name));
        }

        var existed = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(
            p => p.SupermarketId == supermarketId && p.Barcode == request.Barcode);
        if (existed != null)
        {
            throw new InvalidOperationException(
                $"Product with barcode {request.Barcode} already exists in this supermarket (ProductId: {existed.ProductId}).");
        }

        if (request.IsManualFallback)
        {
            _logger.LogInformation(
                "Staff manual fallback: creating verified product {Barcode} without AI OCR pipeline",
                request.Barcode);
        }

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            SupermarketId = supermarketId,
            Name = request.Name.Trim(),
            Barcode = request.Barcode.Trim(),
            CreatedBy = staffName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            VerifiedBy = staffName,
            VerifiedAt = DateTime.UtcNow,
            Status = ProductState.Verified
        };

        var category = await ResolveCategoryByNameAsync(request.CategoryName, cancellationToken);
        if (category != null)
        {
            product.CategoryId = category.CategoryId;
        }

        await _unitOfWork.ProductRepository.AddAsync(product);

        var detail = request.Detail ?? new ProductDetailRequestDto();
        var productDetail = new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = product.ProductId,
            Brand = detail.Brand,
            Ingredients = SerializeIngredientsForStorage(ParseIngredients(detail.Ingredients)),
            NutritionFacts = detail.NutritionFactsJson,
            Manufacturer = detail.Manufacturer,
            Origin = detail.Origin,
            Description = detail.Description,
            StorageInstructions = detail.StorageInstructions,
            UsageInstructions = detail.UsageInstructions,
            SafetyWarning = detail.SafetyWarnings
        };
        await _unitOfWork.Repository<ProductDetail>().AddAsync(productDetail);

        var verificationRawData = request.IsManualFallback
            ? (string.IsNullOrWhiteSpace(request.OcrExtractedData)
                ? """{"source":"manual_fallback"}"""
                : request.OcrExtractedData)
            : request.OcrExtractedData;

        await _unitOfWork.Repository<AIVerificationLog>().AddAsync(new AIVerificationLog
        {
            VerificationId = Guid.NewGuid(),
            ProductId = product.ProductId,
            RawData = verificationRawData,
            ConfidenceScore = (decimal)(request.OcrConfidence ?? 0),
            ExtractedName = request.Name,
            ExtractedBarcode = request.Barcode,
            VerifiedAt = DateTime.UtcNow,
            VerifiedBy = staffName
        });

        if (!string.IsNullOrWhiteSpace(request.OcrImageUrl))
        {
            await _unitOfWork.Repository<ProductImage>().AddAsync(new ProductImage
            {
                ProductImageId = Guid.NewGuid(),
                ProductId = product.ProductId,
                ImageUrl = request.OcrImageUrl,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = true
            });
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateNewProductResponseDto
        {
            ProductId = product.ProductId,
            SupermarketId = product.SupermarketId,
            Name = product.Name,
            Brand = productDetail.Brand ?? string.Empty,
            Category = category?.Name ?? request.CategoryName,
            Barcode = product.Barcode,
            Manufacturer = productDetail.Manufacturer,
            Ingredients = ParseIngredients(productDetail.Ingredients),
            MainImageUrl = request.OcrImageUrl,
            Status = ProductState.Verified,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            IsManualFallback = request.IsManualFallback,
            NextAction = "CREATE_STOCKLOT",
            NextActionDescription = "Sản phẩm đã xác nhận. Tiếp tục tạo lô hàng và gợi ý giá."
        };
    }

    public async Task<StaffCreateLotAndPublishResponseDto> CreateLotAndPublishForStaffAsync(
        StaffCreateLotAndPublishRequestDto request,
        Guid supermarketId,
        string staffName,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(request.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {request.ProductId} not found");
        }

        if (product.SupermarketId != supermarketId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác sản phẩm của siêu thị khác.");
        }

        var createdLot = await CreateStockLotFromExistingAsync(new CreateStockLotFromExistingDto
        {
            ProductId = request.ProductId,
            ExpiryDate = EnsureUtc(request.ExpiryDate),
            ManufactureDate = request.ManufactureDate.HasValue ? EnsureUtc(request.ManufactureDate.Value) : null,
            Quantity = request.Quantity,
            Weight = request.Weight,
            CreatedBy = staffName
        }, cancellationToken);

        PricingSuggestionResponseDto pricingSuggestion;
        if (request.IsManualFallback)
        {
            _logger.LogInformation(
                "Manual pricing fallback for lot workflow: product {ProductId}, lot {LotId} — skipping AI pricing",
                request.ProductId,
                createdLot.LotId);
            pricingSuggestion = await ApplyManualLotPricingAsync(
                createdLot.LotId,
                product,
                request.OriginalUnitPrice,
                cancellationToken);
        }
        else
        {
            pricingSuggestion = await GetLotPricingSuggestionAsync(createdLot.LotId, new GetPricingSuggestionRequestDto
            {
                OriginalPrice = request.OriginalUnitPrice
            }, cancellationToken);
        }

        var acceptedSuggestion = request.AcceptedSuggestion ?? !request.FinalUnitPrice.HasValue;
        var confirmedLot = await ConfirmLotPriceAsync(createdLot.LotId, new ConfirmPriceRequestDto
        {
            FinalPrice = request.FinalUnitPrice,
            AcceptedSuggestion = acceptedSuggestion,
            PriceFeedback = request.PriceFeedback,
            ConfirmedBy = staffName
        }, cancellationToken);

        var publishedLot = await PublishStockLotAsync(confirmedLot.LotId, new PublishProductRequestDto
        {
            PublishedBy = staffName
        }, cancellationToken);

        return new StaffCreateLotAndPublishResponseDto
        {
            ProductId = request.ProductId,
            LotId = publishedLot.LotId,
            PricingSuggestion = pricingSuggestion,
            StockLot = publishedLot,
            IsManualFallback = request.IsManualFallback,
            TimeoutInfo = new WorkflowTimeoutInfoDto
            {
                TimeoutSeconds = WorkflowAiTimeoutSeconds,
                IsAiStep = !request.IsManualFallback,
                SupportsManualFallback = true
            }
        };
    }

    public async Task<StockLotResponseDto> CreateStockLotFromExistingAsync(
        CreateStockLotFromExistingDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating StockLot from existing product {ProductId}", request.ProductId);
        var expiryUtc = EnsureUtc(request.ExpiryDate);
        var manufactureUtc = request.ManufactureDate.HasValue ? EnsureUtc(request.ManufactureDate.Value) : (DateTime?)null;

        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(request.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {request.ProductId} not found");
        }

        if (product.Status != ProductState.Verified)
        {
            throw new InvalidOperationException(
                $"Product must be in Verified status to create StockLot. Current status: {product.Status}. " +
                $"Please verify the product first using POST /api/products/{{productId}}/verify");
        }

        if (!await HasRequiredShelfLifeAsync(expiryUtc, cancellationToken))
        {
            var minHours = await GetMinimumShelfLifeHoursAsync(cancellationToken);
            throw new InvalidOperationException($"Cannot create StockLot. Remaining shelf life must be > {minHours} hours.");
        }

        var defaultUnit = (await _unitOfWork.Repository<UnitOfMeasure>().GetAllAsync()).FirstOrDefault();
        if (defaultUnit == null)
            throw new InvalidOperationException("No Unit found in database. Please seed Units first.");

        var stockLot = new StockLot
        {
            LotId = Guid.NewGuid(),
            ProductId = product.ProductId,
            UnitId = defaultUnit.UnitId,
            ExpiryDate = expiryUtc,
            ManufactureDate = manufactureUtc ?? DateTime.UtcNow,
            Quantity = request.Quantity,
            Weight = request.Weight,
            Status = ProductState.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<StockLot>().AddAsync(stockLot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created StockLot {LotId} for product {ProductId}", stockLot.LotId, product.ProductId);

        return MapToLotResponseDto(stockLot, product);
    }

    public async Task<OcrAnalysisResponseDto> AnalyzeProductImageAsync(
        Guid supermarketId,
        Stream imageStream,
        string fileName,
        string contentType,
        bool skipAi = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing product image for supermarket {SupermarketId}, SkipAi={SkipAi}",
            supermarketId,
            skipAi);

        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);
        if (supermarket == null)
        {
            throw new ArgumentException($"Supermarket with ID {supermarketId} not found.", nameof(supermarketId));
        }

        var r2Storage = _serviceProvider.GetService<IR2StorageService>()
            ?? throw new InvalidOperationException("R2 storage service is not configured. OCR image upload is unavailable.");

        var uploadResult = await r2Storage.UploadToR2Async(
            imageStream,
            fileName,
            contentType,
            cancellationToken);
        var uploadedImageUrl = uploadResult.GetType().GetProperty("Url")?.GetValue(uploadResult)?.ToString();
        if (string.IsNullOrWhiteSpace(uploadedImageUrl))
        {
            throw new InvalidOperationException("Không thể upload ảnh OCR lên R2.");
        }

        var imageUrl = r2Storage.GetPreSignedUrlForImage(uploadedImageUrl, TimeSpan.FromHours(1));
        if (string.IsNullOrEmpty(imageUrl))
        {
            imageUrl = uploadedImageUrl;
        }

        var response = new OcrAnalysisResponseDto
        {
            ImageUrl = uploadedImageUrl,
            ExtractedInfo = new OcrExtractedInfoDto(),
            Confidence = 0,
            AiSkipped = skipAi
        };

        if (skipAi)
        {
            _logger.LogInformation("Skipping AI OCR (manual fallback); image uploaded to {Url}", uploadedImageUrl);
            return response;
        }

        try
        {
            var ocrResult = await _aiClient.ExtractFromUrlAsync(imageUrl, cancellationToken);

            if (ocrResult != null)
            {
                response.Confidence = ocrResult.Confidence;
                response.RawOcrData = JsonSerializer.Serialize(ocrResult);

                var ingredientsStr = ocrResult.ProductInfo?.Ingredients != null
                    ? string.Join(", ", ocrResult.ProductInfo.Ingredients)
                    : null;

                var manufacturerStr = ocrResult.ProductInfo?.Manufacturer?.Name;

                Dictionary<string, string>? nutritionFacts = null;
                if (ocrResult.ProductInfo?.NutritionFacts != null)
                {
                    nutritionFacts = new Dictionary<string, string>();
                    foreach (var kvp in ocrResult.ProductInfo.NutritionFacts)
                    {
                        nutritionFacts[kvp.Key] = kvp.Value?.ToString() ?? "";
                    }
                }

                response.ExtractedInfo = new OcrExtractedInfoDto
                {
                    Name = ocrResult.ProductInfo?.Name ?? ocrResult.Name,
                    Brand = ocrResult.ProductInfo?.Brand ?? ocrResult.Brand,
                    Barcode = ocrResult.ProductInfo?.Barcode ?? ocrResult.Barcode,
                    Category = ocrResult.ProductInfo?.DetectedCategory?.Name,
                    ExpiryDate = ocrResult.ExpiryDate?.Value,
                    ManufactureDate = ocrResult.ManufacturedDate?.Value,
                    Weight = ocrResult.ProductInfo?.Weight ?? ocrResult.ProductInfo?.WeightInfo?.Raw,
                    Ingredients = ParseIngredients(ingredientsStr),
                    Manufacturer = manufacturerStr,
                    Origin = ocrResult.ProductInfo?.Origin,
                    NutritionFacts = nutritionFacts
                };

                if (!string.IsNullOrEmpty(response.ExtractedInfo.Barcode))
                {
                    try
                    {
                        var barcodeLookupInfo = await _barcodeLookupService.LookupAsync(response.ExtractedInfo.Barcode, cancellationToken);
                        if (barcodeLookupInfo != null)
                        {
                            response.BarcodeLookupInfo = new BarcodeLookupInfoDto
                            {
                                Barcode = barcodeLookupInfo.Barcode,
                                ProductName = barcodeLookupInfo.ProductName,
                                Brand = barcodeLookupInfo.Brand,
                                Category = barcodeLookupInfo.Category,
                                Description = barcodeLookupInfo.Description,
                                ImageUrl = barcodeLookupInfo.ImageUrl,
                                Manufacturer = barcodeLookupInfo.Manufacturer,
                                Weight = barcodeLookupInfo.Weight,
                                Ingredients = ParseIngredients(barcodeLookupInfo.Ingredients),
                                NutritionFacts = barcodeLookupInfo.NutritionFacts,
                                Country = barcodeLookupInfo.Country,
                                Source = barcodeLookupInfo.Source,
                                Confidence = barcodeLookupInfo.Confidence,
                                IsVietnameseProduct = barcodeLookupInfo.IsVietnameseProduct,
                                Gs1Prefix = barcodeLookupInfo.Gs1Prefix
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error looking up barcode {Barcode}", response.ExtractedInfo.Barcode);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI OCR");
            response.ExtractedInfo.Name = "OCR Error - Manual Entry Required";
        }

        return response;
    }

    public async Task<CreateNewProductResponseDto> CreateNewProductAsync(
        CreateNewProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new product (Draft) for supermarket {SupermarketId}", request.SupermarketId);

        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(s => s.SupermarketId == request.SupermarketId);
        if (supermarket == null)
        {
            throw new ArgumentException($"Supermarket with ID {request.SupermarketId} not found.", nameof(request.SupermarketId));
        }

        if (!string.IsNullOrEmpty(request.Barcode))
        {
            var existingProduct = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(
                p => p.SupermarketId == request.SupermarketId && p.Barcode == request.Barcode);
            if (existingProduct != null)
            {
                throw new InvalidOperationException(
                    $"Product with barcode {request.Barcode} already exists in this supermarket (ProductId: {existingProduct.ProductId}). Use CreateStockLotFromExisting instead.");
            }
        }

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            SupermarketId = request.SupermarketId,
            Name = request.Name,
            Barcode = request.Barcode,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = ProductState.Draft,
        };

        await _unitOfWork.ProductRepository.AddAsync(product);

        var category = await ResolveCategoryByNameAsync(request.CategoryName, cancellationToken);
        if (category != null)
        {
            product.CategoryId = category.CategoryId;
        }

        var productDetail = new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = product.ProductId,
            Brand = request.Detail.Brand,
            Ingredients = SerializeIngredientsForStorage(ParseIngredients(request.Detail.Ingredients)),
            NutritionFacts = request.Detail.NutritionFactsJson,
            Manufacturer = request.Detail.Manufacturer,
            Origin = request.Detail.Origin,
            Description = request.Detail.Description,
            StorageInstructions = request.Detail.StorageInstructions,
            UsageInstructions = request.Detail.UsageInstructions,
            SafetyWarning = request.Detail.SafetyWarnings
        };
        await _unitOfWork.Repository<ProductDetail>().AddAsync(productDetail);

        string? mainImageUrl = null;
        if (!string.IsNullOrEmpty(request.OcrImageUrl))
        {
            var productImage = new ProductImage
            {
                ProductImageId = Guid.NewGuid(),
                ProductId = product.ProductId,
                ImageUrl = request.OcrImageUrl,
                CreatedAt = DateTime.UtcNow,
                IsPrimary = true
            };
            await _unitOfWork.Repository<ProductImage>().AddAsync(productImage);
            mainImageUrl = request.OcrImageUrl;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new product {ProductId} (Draft status, awaiting verification)", product.ProductId);

        return new CreateNewProductResponseDto
        {
            ProductId = product.ProductId,
            SupermarketId = product.SupermarketId,
            Name = product.Name,
            Brand = productDetail.Brand ?? string.Empty,
            Category = category?.Name ?? request.CategoryName,
            Barcode = product.Barcode,
            Manufacturer = productDetail.Manufacturer,
            Ingredients = ParseIngredients(productDetail.Ingredients),
            MainImageUrl = mainImageUrl,
            Status = ProductState.Draft,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            IsManualFallback = false,
            NextAction = "VERIFY_PRODUCT",
            NextActionDescription = $"Xác nhận thông tin sản phẩm: POST /api/products/{product.ProductId}/verify"
        };
    }

    private async Task<PricingSuggestionResponseDto> ApplyManualLotPricingAsync(
        Guid lotId,
        Product product,
        decimal originalUnitPrice,
        CancellationToken cancellationToken)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId);
        if (lot == null)
            throw new KeyNotFoundException($"StockLot {lotId} not found");

        var daysToExpiry = (int)(lot.ExpiryDate - DateTime.UtcNow).TotalDays;

        var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(h => h.LotId == lotId);
        if (priceHistory == null)
        {
            priceHistory = new PricingHistory
            {
                AIPriceId = Guid.NewGuid(),
                LotId = lotId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<PricingHistory>().AddAsync(priceHistory);
        }

        priceHistory.SuggestedPrice = originalUnitPrice;
        priceHistory.AIConfidence = 0;
        priceHistory.Reason = "Manual fallback (AI pricing skipped)";
        priceHistory.MarketMinPrice = null;
        priceHistory.MarketAvgPrice = null;
        priceHistory.MarketMaxPrice = null;
        _unitOfWork.Repository<PricingHistory>().Update(priceHistory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PricingSuggestionResponseDto
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            OriginalPrice = originalUnitPrice,
            SuggestedPrice = originalUnitPrice,
            Confidence = 0,
            DiscountPercent = 0,
            ExpiryDate = lot.ExpiryDate,
            DaysToExpiry = daysToExpiry,
            Reasons = new List<string> { "Manual fallback (AI pricing skipped)" },
            MinMarketPrice = null,
            AvgMarketPrice = null,
            MaxMarketPrice = null,
            MarketPriceSources = new List<MarketPriceSourceDto>()
        };
    }

    public async Task<PricingSuggestionResponseDto> GetLotPricingSuggestionAsync(
        Guid lotId,
        GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId);
        if (lot == null)
        {
            throw new KeyNotFoundException($"StockLot {lotId} not found");
        }

        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(lot.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {lot.ProductId} not found");
        }

        var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(
            h => h.LotId == lotId);

        if (priceHistory == null)
        {
            priceHistory = new PricingHistory
            {
                AIPriceId = Guid.NewGuid(),
                LotId = lotId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<PricingHistory>().AddAsync(priceHistory);
        }
        _unitOfWork.Repository<PricingHistory>().Update(priceHistory);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var suggestion = await GetPricingSuggestionInternalAsync(product, request.OriginalPrice, cancellationToken);

        priceHistory.SuggestedPrice = suggestion.SuggestedPrice;
        priceHistory.AIConfidence = (decimal)suggestion.Confidence;
        priceHistory.Reason = string.Join("; ", suggestion.Reasons);
        priceHistory.MarketMinPrice = suggestion.MinMarketPrice;
        priceHistory.MarketAvgPrice = suggestion.AvgMarketPrice;
        priceHistory.MarketMaxPrice = suggestion.MaxMarketPrice;
        _unitOfWork.Repository<PricingHistory>().Update(priceHistory);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return suggestion;
    }

    public async Task<StockLotResponseDto> ConfirmLotPriceAsync(
        Guid lotId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId);
        if (lot == null)
        {
            throw new KeyNotFoundException($"StockLot {lotId} not found");
        }

        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(lot.ProductId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {lot.ProductId} not found");
        }

        var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(h => h.LotId == lotId);
        if (priceHistory == null)
        {
            throw new InvalidOperationException("Please get pricing suggestion first.");
        }

        var finalPrice = request.FinalPrice ?? priceHistory.SuggestedPrice;
        priceHistory.AcceptedSuggestion = request.AcceptedSuggestion;
        priceHistory.Feedback = request.PriceFeedback;
        priceHistory.ConfirmedBy = request.ConfirmedBy;
        priceHistory.ConfirmedAt = DateTime.UtcNow;

        _unitOfWork.Repository<PricingHistory>().Update(priceHistory);

        lot.Status = ProductState.Priced;
        _unitOfWork.Repository<StockLot>().Update(lot);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToLotResponseDto(lot, product, priceHistory);
    }

    public async Task<StockLotResponseDto> PublishStockLotAsync(
        Guid lotId,
        PublishProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId);
        if (lot == null)
        {
            throw new KeyNotFoundException($"StockLot {lotId} not found");
        }

        if (lot.Status != ProductState.Priced)
        {
            throw new InvalidOperationException($"StockLot must be in Priced status to publish. Current: {lot.Status}");
        }

        if (!await HasRequiredShelfLifeAsync(lot.ExpiryDate, cancellationToken))
        {
            var minHours = await GetMinimumShelfLifeHoursAsync(cancellationToken);
            throw new InvalidOperationException($"Cannot publish StockLot. Remaining shelf life must be > {minHours} hours.");
        }

        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(lot.ProductId);

        lot.PublishedBy = request.PublishedBy;
        lot.PublishedAt = DateTime.UtcNow;
        lot.Status = ProductState.Published;

        _unitOfWork.Repository<StockLot>().Update(lot);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(h => h.LotId == lotId);
        return MapToLotResponseDto(lot, product, priceHistory);
    }

    public async Task<StockLotResponseDto?> GetStockLotAsync(
        Guid lotId,
        CancellationToken cancellationToken = default)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(l => l.LotId == lotId);
        if (lot == null) return null;

        var product = await _unitOfWork.ProductRepository.GetByIdWithWorkflowDetailsAsync(lot.ProductId);
        var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(h => h.LotId == lotId);

        return MapToLotResponseDto(lot, product, priceHistory);
    }

    public async Task<IEnumerable<StockLotResponseDto>> GetStockLotsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.ProductRepository.FindAsync(p => p.SupermarketId == supermarketId);
        var productIds = products.Select(p => p.ProductId).ToList();

        var lots = await _unitOfWork.Repository<StockLot>().FindAsync(
            l => productIds.Contains(l.ProductId) && l.Status == status);

        var result = new List<StockLotResponseDto>();
        foreach (var lot in lots)
        {
            var product = products.FirstOrDefault(p => p.ProductId == lot.ProductId);
            var priceHistory = await _unitOfWork.Repository<PricingHistory>().FirstOrDefaultAsync(h => h.LotId == lot.LotId);
            result.Add(MapToLotResponseDto(lot, product, priceHistory));
        }

        return result;
    }

    private StockLotResponseDto MapToLotResponseDto(StockLot lot, Product? product, PricingHistory? priceHistory = null)
    {
        var images = product?.ProductImages?.OrderByDescending(i => i.CreatedAt).FirstOrDefault();

        return new StockLotResponseDto
        {
            LotId = lot.LotId,
            ProductId = lot.ProductId,
            ProductName = product?.Name ?? "",
            ProductBarcode = product?.Barcode ?? "",
            ProductBrand = product?.ProductDetail?.Brand,
            ProductImageUrl = images?.ImageUrl,
            ExpiryDate = lot.ExpiryDate,
            ManufactureDate = lot.ManufactureDate,
            DaysToExpiry = (int)(lot.ExpiryDate - DateTime.UtcNow).TotalDays,
            Quantity = lot.Quantity,
            Weight = lot.Weight,
            Status = lot.Status,
            CreatedAt = lot.CreatedAt,
            PublishedBy = lot.PublishedBy,
            PublishedAt = lot.PublishedAt,
            OriginalPrice = lot?.OriginalUnitPrice,
            SuggestedPrice = priceHistory?.SuggestedPrice,
            FinalPrice = null,
            PricingConfidence = priceHistory != null ? (float?)priceHistory.AIConfidence : null
        };
    }

    private async Task<Category?> ResolveCategoryByNameAsync(string? categoryName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        var normalized = categoryName.Trim().ToLower();
        return await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == normalized);
    }

    private async Task<int> GetMinimumShelfLifeHoursAsync(CancellationToken cancellationToken)
    {
        const int defaultHours = 24;

        var config = await _unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(c => c.ConfigKey == "MIN_PUBLISH_SHELF_LIFE_HOURS");

        if (config == null)
        {
            return defaultHours;
        }

        return int.TryParse(config.ConfigValue, out var parsed) && parsed > 0
            ? parsed
            : defaultHours;
    }

    private async Task<bool> HasRequiredShelfLifeAsync(DateTime expiryDate, CancellationToken cancellationToken)
    {
        var minHours = await GetMinimumShelfLifeHoursAsync(cancellationToken);
        return (expiryDate - DateTime.UtcNow).TotalHours > minHours;
    }

    private async Task<StockLot?> GetLatestStockLotByProductIdAsync(Guid productId)
    {
        var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => l.ProductId == productId);
        return lots.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
    }

    private async Task<PricingHistory?> GetLatestPricingHistoryByLotIdAsync(Guid? lotId)
    {
        if (!lotId.HasValue)
        {
            return null;
        }

        var histories = await _unitOfWork.Repository<PricingHistory>().FindAsync(h => h.LotId == lotId.Value);
        return histories.OrderByDescending(h => h.ConfirmedAt ?? h.CreatedAt).FirstOrDefault();
    }

    private async Task<AIVerificationLog?> GetLatestVerificationLogByProductIdAsync(Guid productId)
    {
        var logs = await _unitOfWork.Repository<AIVerificationLog>().FindAsync(x => x.ProductId == productId);
        return logs.OrderByDescending(x => x.VerifiedAt ?? DateTime.MinValue).FirstOrDefault();
    }

    private static List<string> ParseIngredients(string? ingredientsRaw)
    {
        if (string.IsNullOrWhiteSpace(ingredientsRaw))
            return new List<string>();

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(ingredientsRaw);
            if (parsed != null)
            {
                return parsed.Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }
        catch
        {
            // Fallback to plain text split for legacy rows.
        }

        return ingredientsRaw
            .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string SerializeIngredientsForStorage(List<string> ingredients)
    {
        return JsonSerializer.Serialize(ingredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    private static (string FreshnessLevel, float? FreshnessScore) ExtractFreshnessFromRawData(string? rawData)
    {
        const string defaultLevel = "acceptable";
        if (string.IsNullOrWhiteSpace(rawData))
            return (defaultLevel, null);

        try
        {
            using var doc = JsonDocument.Parse(rawData);
            if (!doc.RootElement.TryGetProperty("freshness", out var freshnessNode) || freshnessNode.ValueKind != JsonValueKind.Object)
                return (defaultLevel, null);

            var level = defaultLevel;
            float? score = null;

            if (freshnessNode.TryGetProperty("level", out var levelNode) && levelNode.ValueKind == JsonValueKind.String)
            {
                level = levelNode.GetString() ?? defaultLevel;
            }

            if (freshnessNode.TryGetProperty("score", out var scoreNode))
            {
                if (scoreNode.ValueKind == JsonValueKind.Number && scoreNode.TryGetSingle(out var scoreValue))
                    score = scoreValue;
            }

            return (level, score);
        }
        catch
        {
            return (defaultLevel, null);
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

}



