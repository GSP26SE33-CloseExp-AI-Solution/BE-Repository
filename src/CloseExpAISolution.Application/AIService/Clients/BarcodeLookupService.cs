using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Clients;

/// <summary>
/// Service for looking up product information from barcode.
/// Implements Cache & Crowd-source mechanism:
/// 
/// Flow:
/// 1. Check memory cache (hot cache)
/// 2. Check database (persistent cache)
/// 3. If not found, call external APIs
/// 4. Save results to database for future lookups
/// 5. Support manual entry and AI OCR contributions
/// 
/// API Sources (fallback order):
/// - Open Food Facts Vietnam (vn.openfoodfacts.org)
/// - Open Food Facts Global (world.openfoodfacts.org)
/// - UPCitemdb (free tier: 100 requests/day)
/// </summary>
public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BarcodeLookupService> _logger;
    
    // API Endpoints
    private const string OPEN_FOOD_FACTS_API = "https://world.openfoodfacts.org/api/v2/product/";
    private const string OPEN_FOOD_FACTS_VN_API = "https://vn.openfoodfacts.org/api/v2/product/";
    private const string UPCITEMDB_API = "https://api.upcitemdb.com/prod/trial/lookup?upc=";
    
    private const string CACHE_PREFIX = "barcode_";
    private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromHours(1); // Memory cache for hot data
    private static readonly TimeSpan NotFoundCacheDuration = TimeSpan.FromMinutes(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    // GS1 Country Prefixes for Southeast Asia and common trading partners
    private static readonly Dictionary<string, string> Gs1CountryPrefixes = new()
    {
        { "893", "Vietnam" },
        { "885", "Thailand" },
        { "888", "Singapore" },
        { "890", "India" },
        { "899", "Indonesia" },
        { "955", "Malaysia" },
        { "471", "Taiwan" },
        { "489", "Hong Kong" },
        { "690", "China" }, { "691", "China" }, { "692", "China" }, { "693", "China" }, 
        { "694", "China" }, { "695", "China" }, { "696", "China" }, { "697", "China" },
        { "450", "Japan" }, { "451", "Japan" }, { "452", "Japan" }, { "453", "Japan" },
        { "454", "Japan" }, { "455", "Japan" }, { "456", "Japan" }, { "457", "Japan" },
        { "458", "Japan" }, { "459", "Japan" }, { "490", "Japan" }, { "491", "Japan" },
        { "492", "Japan" }, { "493", "Japan" }, { "494", "Japan" }, { "495", "Japan" },
        { "496", "Japan" }, { "497", "Japan" }, { "498", "Japan" }, { "499", "Japan" },
        { "880", "South Korea" },
        { "000", "USA" }, { "001", "USA" }, { "002", "USA" }, { "003", "USA" },
        { "004", "USA" }, { "005", "USA" }, { "006", "USA" }, { "007", "USA" },
        { "008", "USA" }, { "009", "USA" }, { "010", "USA" }, { "011", "USA" },
        { "012", "USA" }, { "013", "USA" }, { "019", "USA" },
    };

    public BarcodeLookupService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IUnitOfWork unitOfWork,
        ILogger<BarcodeLookupService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BarcodeLookup");
        _cache = cache;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsVietnameseBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        var cleaned = NormalizeBarcode(barcode);
        return cleaned.StartsWith("893");
    }

    /// <inheritdoc/>
    public async Task<BarcodeProductInfo?> LookupAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        barcode = NormalizeBarcode(barcode);
        var cacheKey = $"{CACHE_PREFIX}{barcode}";

        // 1. Check memory cache first (hot cache)
        if (_cache.TryGetValue(cacheKey, out BarcodeProductInfo? cachedResult))
        {
            _logger.LogDebug("Barcode {Barcode} found in memory cache", barcode);
            return cachedResult;
        }

        // 2. Check database (persistent cache)
        var dbProduct = await _unitOfWork.BarcodeProductRepository.GetByBarcodeAsync(barcode);
        if (dbProduct != null)
        {
            _logger.LogInformation("Barcode {Barcode} found in database (Source: {Source})", barcode, dbProduct.Source);
            
            // Increment scan count asynchronously
            _ = Task.Run(async () => 
            {
                try
                {
                    await _unitOfWork.BarcodeProductRepository.IncrementScanCountAsync(barcode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to increment scan count for {Barcode}", barcode);
                }
            }, cancellationToken);

            var result = MapToProductInfo(dbProduct);
            result.Source = "database"; // Override source to indicate from DB
            
            // Cache in memory for quick access
            _cache.Set(cacheKey, result, MemoryCacheDuration);
            
            return result;
        }

        // 3. Not in DB, call external APIs
        _logger.LogInformation("Barcode {Barcode} not in database, looking up from external APIs", barcode);

        BarcodeProductInfo? apiResult = null;
        var isVietnamese = IsVietnameseBarcode(barcode);

        try
        {
            // Strategy: Try multiple sources in order of reliability
            
            // 3a. For Vietnamese products, try VN-specific Open Food Facts first
            if (isVietnamese)
            {
                apiResult = await LookupFromOpenFoodFactsVnAsync(barcode, cancellationToken);
            }

            // 3b. Try global Open Food Facts
            if (apiResult == null)
            {
                apiResult = await LookupFromOpenFoodFactsAsync(barcode, cancellationToken);
            }

            // 3c. Try UPCitemdb as fallback
            if (apiResult == null)
            {
                apiResult = await LookupFromUpcItemDbAsync(barcode, cancellationToken);
            }

            // 4. If found from API, save to database
            if (apiResult != null)
            {
                apiResult.IsVietnameseProduct = isVietnamese;
                apiResult.Gs1Prefix = GetGs1Prefix(barcode);
                
                if (string.IsNullOrEmpty(apiResult.Country))
                {
                    apiResult.Country = GetCountryFromBarcode(barcode);
                }

                // Save to database for future lookups
                await SaveToDatabase(apiResult, cancellationToken);
                
                // Cache in memory
                _cache.Set(cacheKey, apiResult, MemoryCacheDuration);
                
                _logger.LogInformation(
                    "Found and saved product for barcode {Barcode}: {ProductName} (Source: {Source})", 
                    barcode, apiResult.ProductName, apiResult.Source);
            }
            else
            {
                // Cache null result to avoid repeated API calls
                _cache.Set(cacheKey, (BarcodeProductInfo?)null, NotFoundCacheDuration);
                _logger.LogWarning("No product found for barcode {Barcode} in any source", barcode);
            }

            return apiResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up barcode {Barcode}", barcode);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, BarcodeProductInfo?>> LookupBatchAsync(
        IEnumerable<string> barcodes, 
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, BarcodeProductInfo?>();
        var semaphore = new SemaphoreSlim(3); // Limit concurrent requests
        
        var tasks = barcodes.Select(async barcode =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var result = await LookupAsync(barcode, cancellationToken);
                return (barcode, result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var lookupResults = await Task.WhenAll(tasks);
        
        foreach (var (barcode, result) in lookupResults)
        {
            results[barcode] = result;
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<BarcodeProductInfo> SaveProductAsync(
        BarcodeProductInfo productInfo, 
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var barcode = NormalizeBarcode(productInfo.Barcode);
        
        // Check if already exists
        var existing = await _unitOfWork.BarcodeProductRepository.GetByBarcodeAsync(barcode);
        if (existing != null)
        {
            // Update existing
            return await UpdateProductAsync(barcode, productInfo, userId, cancellationToken) ?? productInfo;
        }

        // Create new
        var entity = new BarcodeProduct
        {
            BarcodeProductId = Guid.NewGuid(),
            Barcode = barcode,
            ProductName = productInfo.ProductName ?? "Unknown Product",
            Brand = productInfo.Brand,
            Category = productInfo.Category,
            Description = productInfo.Description,
            ImageUrl = productInfo.ImageUrl,
            Manufacturer = productInfo.Manufacturer,
            Weight = productInfo.Weight,
            Ingredients = productInfo.Ingredients,
            NutritionFactsJson = productInfo.NutritionFacts != null 
                ? JsonSerializer.Serialize(productInfo.NutritionFacts) 
                : null,
            Country = productInfo.Country ?? GetCountryFromBarcode(barcode),
            Gs1Prefix = GetGs1Prefix(barcode),
            IsVietnameseProduct = IsVietnameseBarcode(barcode),
            Source = productInfo.Source ?? "manual",
            Confidence = productInfo.Confidence,
            ScanCount = 1,
            IsVerified = false,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Status = productInfo.Source == "manual" || productInfo.Source == "ai-ocr" 
                ? "pending_review" 
                : "active"
        };

        await _unitOfWork.BarcodeProductRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Saved new barcode product: {Barcode} - {ProductName} (Source: {Source}, User: {User})",
            barcode, entity.ProductName, entity.Source, userId ?? "system");

        return MapToProductInfo(entity);
    }

    /// <inheritdoc/>
    public async Task<BarcodeProductInfo?> UpdateProductAsync(
        string barcode,
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        barcode = NormalizeBarcode(barcode);
        
        var entity = await _unitOfWork.BarcodeProductRepository.GetByBarcodeAsync(barcode);
        if (entity == null)
        {
            _logger.LogWarning("Cannot update non-existent barcode: {Barcode}", barcode);
            return null;
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(productInfo.ProductName))
            entity.ProductName = productInfo.ProductName;
        if (!string.IsNullOrWhiteSpace(productInfo.Brand))
            entity.Brand = productInfo.Brand;
        if (!string.IsNullOrWhiteSpace(productInfo.Category))
            entity.Category = productInfo.Category;
        if (!string.IsNullOrWhiteSpace(productInfo.Description))
            entity.Description = productInfo.Description;
        if (!string.IsNullOrWhiteSpace(productInfo.ImageUrl))
            entity.ImageUrl = productInfo.ImageUrl;
        if (!string.IsNullOrWhiteSpace(productInfo.Manufacturer))
            entity.Manufacturer = productInfo.Manufacturer;
        if (!string.IsNullOrWhiteSpace(productInfo.Weight))
            entity.Weight = productInfo.Weight;
        if (!string.IsNullOrWhiteSpace(productInfo.Ingredients))
            entity.Ingredients = productInfo.Ingredients;
        if (productInfo.NutritionFacts != null)
            entity.NutritionFactsJson = JsonSerializer.Serialize(productInfo.NutritionFacts);
        if (!string.IsNullOrWhiteSpace(productInfo.Country))
            entity.Country = productInfo.Country;

        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;
        
        // If manually updated, may need re-verification
        if (userId != null)
        {
            entity.IsVerified = false;
            entity.Status = "pending_review";
        }

        _unitOfWork.BarcodeProductRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Updated barcode product: {Barcode} - {ProductName} (User: {User})",
            barcode, entity.ProductName, userId ?? "system");

        return MapToProductInfo(entity);
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyProductAsync(string barcode, string verifiedBy, CancellationToken cancellationToken = default)
    {
        barcode = NormalizeBarcode(barcode);
        
        var entity = await _unitOfWork.BarcodeProductRepository.GetByBarcodeAsync(barcode);
        if (entity == null)
        {
            return false;
        }

        entity.IsVerified = true;
        entity.Status = "active";
        entity.UpdatedBy = verifiedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.BarcodeProductRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation("Verified barcode product: {Barcode} by {User}", barcode, verifiedBy);

        return true;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BarcodeProductInfo>> SearchAsync(
        string searchTerm, 
        int limit = 20, 
        CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BarcodeProductRepository.SearchAsync(searchTerm, limit);
        return entities.Select(MapToProductInfo);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<BarcodeProductInfo>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _unitOfWork.BarcodeProductRepository.GetPendingReviewAsync();
        return entities.Select(MapToProductInfo);
    }

    #region Private Methods

    /// <summary>
    /// Save API result to database
    /// </summary>
    private async Task SaveToDatabase(BarcodeProductInfo productInfo, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new BarcodeProduct
            {
                BarcodeProductId = Guid.NewGuid(),
                Barcode = productInfo.Barcode,
                ProductName = productInfo.ProductName ?? "Unknown Product",
                Brand = productInfo.Brand,
                Category = productInfo.Category,
                Description = productInfo.Description,
                ImageUrl = productInfo.ImageUrl,
                Manufacturer = productInfo.Manufacturer,
                Weight = productInfo.Weight,
                Ingredients = productInfo.Ingredients,
                NutritionFactsJson = productInfo.NutritionFacts != null 
                    ? JsonSerializer.Serialize(productInfo.NutritionFacts) 
                    : null,
                Country = productInfo.Country,
                Gs1Prefix = productInfo.Gs1Prefix,
                IsVietnameseProduct = productInfo.IsVietnameseProduct,
                Source = productInfo.Source,
                Confidence = productInfo.Confidence,
                ScanCount = 1,
                IsVerified = true, // API data is considered verified
                CreatedBy = null, // System
                CreatedAt = DateTime.UtcNow,
                Status = "active"
            };

            await _unitOfWork.BarcodeProductRepository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save barcode {Barcode} to database", productInfo.Barcode);
            // Don't throw - lookup still succeeded
        }
    }

    /// <summary>
    /// Map database entity to DTO
    /// </summary>
    private BarcodeProductInfo MapToProductInfo(BarcodeProduct entity)
    {
        return new BarcodeProductInfo
        {
            Barcode = entity.Barcode,
            ProductName = entity.ProductName,
            Brand = entity.Brand,
            Category = entity.Category,
            Description = entity.Description,
            ImageUrl = entity.ImageUrl,
            Manufacturer = entity.Manufacturer,
            Weight = entity.Weight,
            Ingredients = entity.Ingredients,
            NutritionFacts = !string.IsNullOrEmpty(entity.NutritionFactsJson)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(entity.NutritionFactsJson)
                : null,
            Country = entity.Country,
            Gs1Prefix = entity.Gs1Prefix,
            IsVietnameseProduct = entity.IsVietnameseProduct,
            Source = entity.Source,
            Confidence = entity.Confidence,
            LookupTimestamp = entity.UpdatedAt ?? entity.CreatedAt,
            ScanCount = entity.ScanCount,
            IsVerified = entity.IsVerified
        };
    }

    /// <summary>
    /// Lookup from Vietnam-specific Open Food Facts database
    /// </summary>
    private async Task<BarcodeProductInfo?> LookupFromOpenFoodFactsVnAsync(
        string barcode, 
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{OPEN_FOOD_FACTS_VN_API}{barcode}.json";
            return await FetchOpenFoodFactsAsync(url, barcode, "openfoodfacts-vn", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "VN Open Food Facts lookup failed for {Barcode}", barcode);
            return null;
        }
    }

    /// <summary>
    /// Lookup from global Open Food Facts database
    /// </summary>
    private async Task<BarcodeProductInfo?> LookupFromOpenFoodFactsAsync(
        string barcode, 
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{OPEN_FOOD_FACTS_API}{barcode}.json";
            return await FetchOpenFoodFactsAsync(url, barcode, "openfoodfacts", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Open Food Facts lookup failed for {Barcode}", barcode);
            return null;
        }
    }

    private async Task<BarcodeProductInfo?> FetchOpenFoodFactsAsync(
        string url, 
        string barcode, 
        string source,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "CloseExpAI/1.0 (Vietnamese Product Scanner)");
        
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var offResponse = JsonSerializer.Deserialize<OpenFoodFactsResponse>(content, JsonOptions);

        if (offResponse?.Status != 1 || offResponse.Product == null)
        {
            return null;
        }

        var product = offResponse.Product;
        
        return new BarcodeProductInfo
        {
            Barcode = barcode,
            ProductName = GetBestProductName(product),
            Brand = product.Brands,
            Category = GetBestCategory(product),
            Description = product.GenericName ?? product.GenericNameVi ?? product.GenericNameEn,
            ImageUrl = product.ImageUrl ?? product.ImageFrontUrl,
            Manufacturer = product.ManufacturerName,
            Weight = product.Quantity,
            Ingredients = product.IngredientsText ?? product.IngredientsTextVi ?? product.IngredientsTextEn,
            NutritionFacts = ExtractNutritionFacts(product),
            Country = product.Countries ?? product.CountriesHierarchy?.FirstOrDefault(),
            Source = source,
            Confidence = 0.9f,
            LookupTimestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Lookup from UPCitemdb (free tier: 100 requests/day)
    /// </summary>
    private async Task<BarcodeProductInfo?> LookupFromUpcItemDbAsync(
        string barcode, 
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{UPCITEMDB_API}{barcode}";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "CloseExpAI/1.0");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var upcResponse = JsonSerializer.Deserialize<UpcItemDbResponse>(content, JsonOptions);

            if (upcResponse?.Code != "OK" || upcResponse.Items == null || !upcResponse.Items.Any())
            {
                return null;
            }

            var item = upcResponse.Items.First();
            
            return new BarcodeProductInfo
            {
                Barcode = barcode,
                ProductName = item.Title,
                Brand = item.Brand,
                Category = item.Category,
                Description = item.Description,
                ImageUrl = item.Images?.FirstOrDefault(),
                Weight = item.Weight,
                Source = "upcitemdb",
                Confidence = 0.8f,
                LookupTimestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "UPCitemdb lookup failed for {Barcode}", barcode);
            return null;
        }
    }

    #endregion

    #region Helper Methods

    private static string NormalizeBarcode(string barcode)
    {
        return new string(barcode.Where(char.IsDigit).ToArray());
    }

    private static string? GetGs1Prefix(string barcode)
    {
        if (string.IsNullOrEmpty(barcode) || barcode.Length < 3)
            return null;
        return barcode.Substring(0, 3);
    }

    private static string? GetCountryFromBarcode(string barcode)
    {
        var prefix = GetGs1Prefix(barcode);
        if (prefix == null) return null;

        if (Gs1CountryPrefixes.TryGetValue(prefix, out var country))
            return country;

        // Check 2-digit prefixes for some countries
        if (barcode.Length >= 2)
        {
            var prefix2 = barcode.Substring(0, 2);
            if (prefix2.StartsWith("3")) return "France";
            if (prefix2.StartsWith("4") && int.TryParse(prefix2, out var p) && p >= 40 && p <= 44) return "Germany";
            if (prefix2 == "50") return "United Kingdom";
        }

        return null;
    }

    private static string? GetBestProductName(OpenFoodFactsProduct product)
    {
        return product.ProductNameVi 
            ?? product.ProductName 
            ?? product.ProductNameEn 
            ?? product.GenericNameVi 
            ?? product.GenericName;
    }

    private static string? GetBestCategory(OpenFoodFactsProduct product)
    {
        if (product.CategoriesHierarchy?.Any() == true)
        {
            return product.CategoriesHierarchy.LastOrDefault()?.Replace("en:", "").Replace("vi:", "");
        }
        return product.Categories?.Split(',').FirstOrDefault()?.Trim();
    }

    private static Dictionary<string, string>? ExtractNutritionFacts(OpenFoodFactsProduct product)
    {
        var nutriments = product.Nutriments;
        if (nutriments == null) return null;

        var facts = new Dictionary<string, string>();

        if (nutriments.EnergyKcal100g.HasValue)
            facts["calories"] = $"{nutriments.EnergyKcal100g}kcal";
        if (nutriments.Proteins100g.HasValue)
            facts["protein"] = $"{nutriments.Proteins100g}g";
        if (nutriments.Fat100g.HasValue)
            facts["fat"] = $"{nutriments.Fat100g}g";
        if (nutriments.Carbohydrates100g.HasValue)
            facts["carbs"] = $"{nutriments.Carbohydrates100g}g";
        if (nutriments.Sugars100g.HasValue)
            facts["sugar"] = $"{nutriments.Sugars100g}g";
        if (nutriments.Sodium100g.HasValue)
            facts["sodium"] = $"{nutriments.Sodium100g}mg";
        if (nutriments.Fiber100g.HasValue)
            facts["fiber"] = $"{nutriments.Fiber100g}g";

        return facts.Count > 0 ? facts : null;
    }

    #endregion
}

#region API Response Models

internal class OpenFoodFactsResponse
{
    [JsonPropertyName("status")]
    public int Status { get; set; }
    
    [JsonPropertyName("status_verbose")]
    public string? StatusVerbose { get; set; }
    
    [JsonPropertyName("product")]
    public OpenFoodFactsProduct? Product { get; set; }
}

internal class OpenFoodFactsProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }
    
    [JsonPropertyName("product_name_vi")]
    public string? ProductNameVi { get; set; }
    
    [JsonPropertyName("product_name_en")]
    public string? ProductNameEn { get; set; }
    
    [JsonPropertyName("generic_name")]
    public string? GenericName { get; set; }
    
    [JsonPropertyName("generic_name_vi")]
    public string? GenericNameVi { get; set; }
    
    [JsonPropertyName("generic_name_en")]
    public string? GenericNameEn { get; set; }
    
    [JsonPropertyName("brands")]
    public string? Brands { get; set; }
    
    [JsonPropertyName("categories")]
    public string? Categories { get; set; }
    
    [JsonPropertyName("categories_hierarchy")]
    public List<string>? CategoriesHierarchy { get; set; }
    
    [JsonPropertyName("countries")]
    public string? Countries { get; set; }
    
    [JsonPropertyName("countries_hierarchy")]
    public List<string>? CountriesHierarchy { get; set; }
    
    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }
    
    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
    
    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }
    
    [JsonPropertyName("ingredients_text")]
    public string? IngredientsText { get; set; }
    
    [JsonPropertyName("ingredients_text_vi")]
    public string? IngredientsTextVi { get; set; }
    
    [JsonPropertyName("ingredients_text_en")]
    public string? IngredientsTextEn { get; set; }
    
    [JsonPropertyName("manufacturer")]
    public string? ManufacturerName { get; set; }
    
    [JsonPropertyName("nutriments")]
    public OpenFoodFactsNutriments? Nutriments { get; set; }
}

internal class OpenFoodFactsNutriments
{
    [JsonPropertyName("energy-kcal_100g")]
    public float? EnergyKcal100g { get; set; }
    
    [JsonPropertyName("proteins_100g")]
    public float? Proteins100g { get; set; }
    
    [JsonPropertyName("fat_100g")]
    public float? Fat100g { get; set; }
    
    [JsonPropertyName("carbohydrates_100g")]
    public float? Carbohydrates100g { get; set; }
    
    [JsonPropertyName("sugars_100g")]
    public float? Sugars100g { get; set; }
    
    [JsonPropertyName("sodium_100g")]
    public float? Sodium100g { get; set; }
    
    [JsonPropertyName("fiber_100g")]
    public float? Fiber100g { get; set; }
}

internal class UpcItemDbResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("items")]
    public List<UpcItemDbItem>? Items { get; set; }
}

internal class UpcItemDbItem
{
    [JsonPropertyName("ean")]
    public string? Ean { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("weight")]
    public string? Weight { get; set; }
    
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }
}

#endregion

