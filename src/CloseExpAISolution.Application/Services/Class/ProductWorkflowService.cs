using System.Text.Json;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

/// <summary>
/// Implementation of Product Workflow Service.
/// Handles the complete product lifecycle: Upload → OCR → Verify → Price → Publish
/// </summary>
public class ProductWorkflowService : IProductWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIServiceClient _aiClient;
    private readonly IR2StorageService _r2Storage;
    private readonly IMarketPriceService _marketPriceService;
    private readonly ILogger<ProductWorkflowService> _logger;

    public ProductWorkflowService(
        IUnitOfWork unitOfWork,
        IAIServiceClient aiClient,
        IR2StorageService r2Storage,
        IMarketPriceService marketPriceService,
        ILogger<ProductWorkflowService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiClient = aiClient;
        _r2Storage = r2Storage;
        _marketPriceService = marketPriceService;
        _logger = logger;
    }

    #region Step 1: Upload & OCR

    public async Task<ProductResponseDto> UploadAndExtractAsync(
        Guid supermarketId,
        string createdBy,
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting upload and extract for supermarket {SupermarketId}", supermarketId);

        // 1. Create product record first to get ProductId
        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            SupermarketId = supermarketId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Status = ProductState.Draft.ToString(),
            Name = "Processing...",
            Brand = "",
            Category = "",
            Barcode = ""
        };

        await _unitOfWork.ProductRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 2. Upload image to R2
        var productImage = await _r2Storage.UploadProductImageToR2Async(
            imageStream,
            fileName,
            contentType,
            product.ProductId,
            cancellationToken);

        // Get pre-signed URL for AI service to access the image (valid for 1 hour)
        var imageUrl = _r2Storage.GetPreSignedUrlForImage(productImage.ImageUrl, TimeSpan.FromHours(1));
        
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogWarning("Could not generate pre-signed URL for image {ImageUrl}", productImage.ImageUrl);
            imageUrl = productImage.ImageUrl; // Fallback to original URL
        }

        // 3. Call AI OCR to extract product info
        try
        {
            var ocrResult = await _aiClient.ExtractFromUrlAsync(imageUrl, cancellationToken);

            if (ocrResult != null)
            {
                // Update product with OCR extracted data
                product.Name = ocrResult.ProductInfo?.Name ?? ocrResult.Name ?? "Unknown Product";
                product.Brand = ocrResult.ProductInfo?.Brand ?? ocrResult.Brand ?? "";
                product.Barcode = ocrResult.ProductInfo?.Barcode ?? ocrResult.Barcode ?? "";
                product.Category = ocrResult.ProductInfo?.DetectedCategory?.Name ?? "";
                product.ExpiryDate = ocrResult.ExpiryDate?.Value;
                product.ManufactureDate = ocrResult.ManufacturedDate?.Value;
                product.ShelfLifeDays = ocrResult.ProductInfo?.ShelfLifeDays;
                product.OcrConfidence = ocrResult.Confidence;
                product.OcrExtractedData = JsonSerializer.Serialize(ocrResult);

                // Determine if fresh food based on category
                product.IsFreshFood = IsFreshFoodCategory(product.Category);

                _unitOfWork.ProductRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("OCR extraction successful for product {ProductId}: {ProductName}",
                    product.ProductId, product.Name);
            }
            else
            {
                _logger.LogWarning("OCR extraction returned null for product {ProductId}", product.ProductId);

                product.Name = "Manual Entry Required";
                product.OcrConfidence = 0;
                _unitOfWork.ProductRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None); // Don't use request token here
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling AI OCR for product {ProductId}", product.ProductId);
            product.Name = "OCR Error - Manual Entry Required";
            product.OcrConfidence = 0;
            _unitOfWork.ProductRepository.Update(product);
            // Use CancellationToken.None to ensure we save even if original request was cancelled
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        return MapToResponseDto(product);
    }

    #endregion

    #region Step 2: Verify Product

    public async Task<PricingSuggestionResponseDto> VerifyProductAsync(
        Guid productId,
        VerifyProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        if (product.Status != ProductState.Draft.ToString())
        {
            throw new InvalidOperationException($"Product must be in Draft status to verify. Current status: {product.Status}");
        }

        _logger.LogInformation("Verifying product {ProductId}", productId);

        // Update product info if provided (corrections to OCR)
        if (!string.IsNullOrEmpty(request.Name))
            product.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Brand))
            product.Brand = request.Brand;
        if (!string.IsNullOrEmpty(request.Category))
            product.Category = request.Category;
        if (!string.IsNullOrEmpty(request.Barcode))
            product.Barcode = request.Barcode;
        if (request.ExpiryDate.HasValue)
            product.ExpiryDate = request.ExpiryDate.Value;
        if (request.ManufactureDate.HasValue)
            product.ManufactureDate = request.ManufactureDate.Value;

        // Set original price
        product.OriginalPrice = request.OriginalPrice;

        // Update verification info
        product.VerifiedBy = request.VerifiedBy;
        product.VerifiedAt = DateTime.UtcNow;
        product.Status = ProductState.Verified.ToString();

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get pricing suggestion
        var pricingSuggestion = await GetPricingSuggestionInternalAsync(product, cancellationToken);

        // Save suggested price to product
        product.SuggestedPrice = (decimal)pricingSuggestion.SuggestedPrice;
        product.PricingConfidence = pricingSuggestion.Confidence;
        product.PricingReasons = JsonSerializer.Serialize(pricingSuggestion.Reasons);

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return pricingSuggestion;
    }

    #endregion

    #region Step 3: Get Pricing Suggestion

    public async Task<PricingSuggestionResponseDto> GetPricingSuggestionAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        if (product.Status == ProductState.Draft.ToString())
        {
            throw new InvalidOperationException("Product must be verified before getting pricing suggestion");
        }

        return await GetPricingSuggestionInternalAsync(product, cancellationToken);
    }

    private async Task<PricingSuggestionResponseDto> GetPricingSuggestionInternalAsync(
        Product product,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting pricing suggestion for product {ProductId}", product.ProductId);

        // Get market prices for comparison
        List<MarketPriceSourceDto> marketSources = new();
        decimal? minMarketPrice = null;
        decimal? avgMarketPrice = null;
        decimal? maxMarketPrice = null;

        if (!string.IsNullOrEmpty(product.Barcode) || !string.IsNullOrEmpty(product.Name))
        {
            try
            {
                // First try to get from database
                var marketPriceResult = await _marketPriceService.GetMarketPriceAsync(
                    product.Barcode ?? "",
                    cancellationToken);

                // If no results in DB, trigger crawl from AI service
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
                        
                        // After crawl, fetch fresh data from DB to get detailed info
                        marketPriceResult = await _marketPriceService.GetMarketPriceAsync(
                            product.Barcode ?? "",
                            cancellationToken);
                        
                        // Also try search by product name if barcode didn't return results
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

                // Map results from DB (whether existing or just crawled)
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

        // Calculate days to expiry
        int? daysToExpiry = null;
        if (product.ExpiryDate.HasValue)
        {
            daysToExpiry = (int)(product.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
        }

        // Call AI pricing API
        try
        {
            // Map category to valid product_type enum
            var productType = MapCategoryToProductType(product.Category);
            
            // Convert expiry date to date only (no time component)
            DateTime? expiryDateOnly = product.ExpiryDate.HasValue 
                ? product.ExpiryDate.Value.Date 
                : null;
            
            var pricingRequest = new PricingRequest
            {
                ProductType = productType,
                DaysToExpire = daysToExpiry ?? 30,
                BasePrice = product.OriginalPrice,
                ExpiryDate = expiryDateOnly,
                Brand = product.Brand,
                MinMarketPrice = minMarketPrice,
                AvgMarketPrice = avgMarketPrice,
                ProductName = product.Name,
                Barcode = product.Barcode
            };
            
            var pricingResult = await _aiClient.GetPriceSuggestionAsync(pricingRequest, cancellationToken);

            if (pricingResult != null)
            {
                var discountPercent = product.OriginalPrice > 0
                    ? (1 - pricingResult.SuggestedPrice / product.OriginalPrice) * 100
                    : 0;

                return new PricingSuggestionResponseDto
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    OriginalPrice = product.OriginalPrice,
                    SuggestedPrice = pricingResult.SuggestedPrice,
                    Confidence = pricingResult.Confidence,
                    DiscountPercent = Math.Round(discountPercent, 1),
                    ExpiryDate = product.ExpiryDate,
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

        // Fallback: simple calculation if AI fails
        var fallbackDiscount = CalculateFallbackDiscount(daysToExpiry);
        var fallbackPrice = product.OriginalPrice * (1 - fallbackDiscount / 100);

        return new PricingSuggestionResponseDto
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            OriginalPrice = product.OriginalPrice,
            SuggestedPrice = fallbackPrice,
            Confidence = 0.5f,
            DiscountPercent = fallbackDiscount,
            ExpiryDate = product.ExpiryDate,
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

    #endregion

    #region Step 4: Confirm Price

    public async Task<ProductResponseDto> ConfirmPriceAsync(
        Guid productId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        if (product.Status != ProductState.Verified.ToString())
        {
            throw new InvalidOperationException($"Product must be in Verified status to confirm price. Current status: {product.Status}");
        }

        _logger.LogInformation("Confirming price for product {ProductId}", productId);

        // Set final price (use provided or suggested)
        product.FinalPrice = request.FinalPrice ?? product.SuggestedPrice;
        product.PricedBy = request.ConfirmedBy;
        product.PricedAt = DateTime.UtcNow;
        product.Status = ProductState.Priced.ToString();

        _unitOfWork.ProductRepository.Update(product);

        // Save price feedback for AI improvement
        if (!string.IsNullOrEmpty(request.PriceFeedback) || !request.AcceptedSuggestion)
        {
            var feedback = new PriceFeedback
            {
                Id = Guid.NewGuid(),
                Barcode = product.Barcode,
                ProductName = product.Name,
                OriginalPrice = product.OriginalPrice,
                SuggestedPrice = product.SuggestedPrice,
                FinalPrice = product.FinalPrice,
                WasAccepted = request.AcceptedSuggestion,
                StaffFeedback = request.PriceFeedback,
                StaffId = request.ConfirmedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PriceFeedbackRepository.AddAsync(feedback, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(product);
    }

    #endregion

    #region Step 5: Publish Product

    public async Task<ProductResponseDto> PublishProductAsync(
        Guid productId,
        PublishProductRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        if (product.Status != ProductState.Priced.ToString())
        {
            throw new InvalidOperationException($"Product must be in Priced status to publish. Current status: {product.Status}");
        }

        _logger.LogInformation("Publishing product {ProductId}", productId);

        product.PublishedBy = request.PublishedBy;
        product.PublishedAt = DateTime.UtcNow;
        product.Status = ProductState.Published.ToString();

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(product);
    }

    #endregion

    #region Query Methods

    public async Task<ProductResponseDto?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        return product == null ? null : MapToResponseDto(product);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetProductsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.ProductRepository.FindAsync(
            p => p.SupermarketId == supermarketId && p.Status == status.ToString());

        return products.Select(MapToResponseDto);
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
            DraftCount = productList.Count(p => p.Status == ProductState.Draft.ToString()),
            VerifiedCount = productList.Count(p => p.Status == ProductState.Verified.ToString()),
            PricedCount = productList.Count(p => p.Status == ProductState.Priced.ToString()),
            PublishedCount = productList.Count(p => p.Status == ProductState.Published.ToString()),
            ExpiredCount = productList.Count(p => p.Status == ProductState.Expired.ToString()),
            TotalCount = productList.Count
        };
    }

    #endregion

    #region Quick Actions

    public async Task<ProductResponseDto> QuickApproveAsync(
        Guid productId,
        QuickApproveRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(p => p.ProductId == productId);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product {productId} not found");
        }

        _logger.LogInformation("Quick approving product {ProductId}", productId);

        // Step 1: Verify
        product.OriginalPrice = request.OriginalPrice;
        product.VerifiedBy = request.StaffId;
        product.VerifiedAt = DateTime.UtcNow;

        // Step 2: Get and apply pricing
        var pricing = await GetPricingSuggestionInternalAsync(product, cancellationToken);
        product.SuggestedPrice = pricing.SuggestedPrice;
        product.PricingConfidence = pricing.Confidence;
        product.PricingReasons = JsonSerializer.Serialize(pricing.Reasons);

        // Step 3: Confirm price
        product.FinalPrice = request.AcceptAiSuggestion
            ? pricing.SuggestedPrice
            : request.FinalPrice ?? pricing.SuggestedPrice;
        product.PricedBy = request.StaffId;
        product.PricedAt = DateTime.UtcNow;

        // Step 4: Publish
        product.PublishedBy = request.StaffId;
        product.PublishedAt = DateTime.UtcNow;
        product.Status = ProductState.Published.ToString();

        _unitOfWork.ProductRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(product);
    }

    #endregion

    #region Helper Methods

    private ProductResponseDto MapToResponseDto(Product product)
    {
        int? daysToExpiry = null;
        if (product.ExpiryDate.HasValue)
        {
            daysToExpiry = (int)(product.ExpiryDate.Value - DateTime.UtcNow).TotalDays;
        }

        return new ProductResponseDto
        {
            ProductId = product.ProductId,
            SupermarketId = product.SupermarketId,
            Name = product.Name,
            Brand = product.Brand,
            Category = product.Category,
            Barcode = product.Barcode,
            IsFreshFood = product.IsFreshFood,
            Status = Enum.TryParse<ProductState>(product.Status, out var state) ? state : ProductState.Draft,
            OriginalPrice = product.OriginalPrice,
            SuggestedPrice = product.SuggestedPrice,
            FinalPrice = product.FinalPrice,
            ExpiryDate = product.ExpiryDate,
            ManufactureDate = product.ManufactureDate,
            DaysToExpiry = daysToExpiry,
            OcrConfidence = product.OcrConfidence,
            PricingConfidence = product.PricingConfidence,
            PricingReasons = product.PricingReasons,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            VerifiedBy = product.VerifiedBy,
            VerifiedAt = product.VerifiedAt,
            PricedBy = product.PricedBy,
            PricedAt = product.PricedAt
        };
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

    /// <summary>
    /// Map product category to valid AI service product_type enum.
    /// Valid values: dairy, meat, seafood, bakery, produce, frozen, beverage, snack, condiment, other
    /// </summary>
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

        // Vietnamese mappings
        var categoryMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Dairy - Sữa
            { "sữa", "dairy" },
            { "sua", "dairy" },
            { "phô mai", "dairy" },
            { "pho mai", "dairy" },
            { "sữa chua", "dairy" },
            { "bơ", "dairy" },
            
            // Meat - Thịt
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
            
            // Seafood - Hải sản
            { "hải sản", "seafood" },
            { "hai san", "seafood" },
            { "cá", "seafood" },
            { "ca", "seafood" },
            { "tôm", "seafood" },
            { "tom", "seafood" },
            { "mực", "seafood" },
            { "cua", "seafood" },
            
            // Bakery - Bánh
            { "bánh", "bakery" },
            { "banh", "bakery" },
            { "bread", "bakery" },
            
            // Produce - Rau củ
            { "rau", "produce" },
            { "củ", "produce" },
            { "cu", "produce" },
            { "quả", "produce" },
            { "qua", "produce" },
            { "trái cây", "produce" },
            { "trai cay", "produce" },
            { "vegetable", "produce" },
            { "fruit", "produce" },
            
            // Frozen - Đông lạnh
            { "đông lạnh", "frozen" },
            { "dong lanh", "frozen" },
            { "kem", "frozen" },
            
            // Beverage - Nước uống
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
            
            // Snack - Đồ ăn vặt
            { "snack", "snack" },
            { "bánh snack", "snack" },
            { "chip", "snack" },
            { "kẹo", "snack" },
            { "keo", "snack" },
            { "chocolate", "snack" },
            { "ăn vặt", "snack" },
            { "an vat", "snack" },
            
            // Condiment - Gia vị
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
            
            // Canned/Processed food - map to other or specific
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

    #endregion
}
