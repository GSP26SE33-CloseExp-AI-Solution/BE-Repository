using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Clients;

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

    public bool IsVietnameseBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return false;
        var cleaned = NormalizeBarcode(barcode);
        return cleaned.StartsWith("893");
    }

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
        var dbProduct = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.Status != ProductState.Hidden);
        if (dbProduct != null)
        {
            var detail = await _unitOfWork.Repository<ProductDetail>()
                .FirstOrDefaultAsync(d => d.ProductId == dbProduct.ProductId);
            var category = dbProduct.CategoryId.HasValue
                ? await _unitOfWork.Repository<Category>()
                    .FirstOrDefaultAsync(c => c.CategoryId == dbProduct.CategoryId.Value)
                : null;

            _logger.LogInformation("Barcode {Barcode} found in product database", barcode);

            var result = MapToProductInfo(dbProduct, detail, category, "database");

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

    public async Task<BarcodeProductInfo> SaveProductAsync(
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var barcode = NormalizeBarcode(productInfo.Barcode);

        // Check if already exists
        var existingProduct = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
        if (existingProduct != null)
        {
            return await UpdateProductAsync(barcode, productInfo, userId, cancellationToken) ?? productInfo;
        }

        var allSupermarkets = await _unitOfWork.Repository<Supermarket>().GetAllAsync();
        var defaultSupermarket = allSupermarkets.FirstOrDefault();
        if (defaultSupermarket == null)
        {
            throw new InvalidOperationException("Không tìm thấy Supermarket mặc định để tạo sản phẩm.");
        }

        var allUnitOfMeasures = await _unitOfWork.Repository<UnitOfMeasure>().GetAllAsync();
        var defaultUnitOfMeasure = allUnitOfMeasures.FirstOrDefault();
        if (defaultUnitOfMeasure == null)
        {
            throw new InvalidOperationException("Không tìm thấy UnitOfMeasure mặc định để tạo sản phẩm.");
        }

        var category = await ResolveCategoryAsync(productInfo.Category);

        var product = new Product
        {
            ProductId = Guid.NewGuid(),
            Name = productInfo.ProductName ?? "Unknown Product",
            Barcode = barcode,
            Sku = barcode,
            CategoryId = category?.CategoryId,
            SupermarketId = defaultSupermarket.SupermarketId,
            Status = ProductState.Verified,
            CreatedBy = userId ?? "system",
            CreatedAt = DateTime.UtcNow,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow,
            VerifiedBy = userId,
            VerifiedAt = DateTime.UtcNow
        };

        var detail = new ProductDetail
        {
            ProductDetailId = Guid.NewGuid(),
            ProductId = product.ProductId,
            Brand = productInfo.Brand,
            Description = productInfo.Description,
            Manufacturer = productInfo.Manufacturer,
            Ingredients = productInfo.Ingredients,
            NutritionFacts = productInfo.NutritionFacts != null
                ? JsonSerializer.Serialize(productInfo.NutritionFacts)
                : null,
            CountryOfOrigin = productInfo.Country
        };

        await _unitOfWork.Repository<Product>().AddAsync(product);
        await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Saved new product from barcode lookup: {Barcode} - {ProductName} (User: {User})",
            barcode, product.Name, userId ?? "system");

        return MapToProductInfo(product, detail, category, productInfo.Source ?? "manual");
    }

    public async Task<BarcodeProductInfo?> UpdateProductAsync(
        string barcode,
        BarcodeProductInfo productInfo,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        barcode = NormalizeBarcode(barcode);

        var product = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
        if (product == null)
        {
            _logger.LogWarning("Cannot update non-existent barcode: {Barcode}", barcode);
            return null;
        }

        var detail = await _unitOfWork.Repository<ProductDetail>()
            .FirstOrDefaultAsync(d => d.ProductId == product.ProductId);
        if (detail == null)
        {
            detail = new ProductDetail
            {
                ProductDetailId = Guid.NewGuid(),
                ProductId = product.ProductId
            };
            await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);
        }

        if (!string.IsNullOrWhiteSpace(productInfo.ProductName))
            product.Name = productInfo.ProductName;
        if (!string.IsNullOrWhiteSpace(productInfo.Brand))
            detail.Brand = productInfo.Brand;
        if (!string.IsNullOrWhiteSpace(productInfo.Category))
        {
            var category = await ResolveCategoryAsync(productInfo.Category);
            product.CategoryId = category?.CategoryId;
        }
        if (!string.IsNullOrWhiteSpace(productInfo.Description))
            detail.Description = productInfo.Description;
        if (!string.IsNullOrWhiteSpace(productInfo.Manufacturer))
            detail.Manufacturer = productInfo.Manufacturer;
        if (!string.IsNullOrWhiteSpace(productInfo.Ingredients))
            detail.Ingredients = productInfo.Ingredients;
        if (productInfo.NutritionFacts != null)
            detail.NutritionFacts = JsonSerializer.Serialize(productInfo.NutritionFacts);
        if (!string.IsNullOrWhiteSpace(productInfo.Country))
            detail.CountryOfOrigin = productInfo.Country;

        product.UpdatedBy = userId;
        product.UpdatedAt = DateTime.UtcNow;

        if (userId != null)
        {
            product.VerifiedAt = null;
            product.VerifiedBy = null;
            product.Status = ProductState.Draft;
        }

        _unitOfWork.Repository<Product>().Update(product);
        _unitOfWork.Repository<ProductDetail>().Update(detail);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation(
            "Updated product by barcode: {Barcode} - {ProductName} (User: {User})",
            barcode, product.Name, userId ?? "system");

        var categoryRef = product.CategoryId.HasValue
            ? await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.CategoryId == product.CategoryId.Value)
            : null;
        return MapToProductInfo(product, detail, categoryRef, "database");
    }

    public async Task<bool> VerifyProductAsync(string barcode, string verifiedBy, CancellationToken cancellationToken = default)
    {
        barcode = NormalizeBarcode(barcode);

        var product = await _unitOfWork.Repository<Product>()
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
        if (product == null)
        {
            return false;
        }

        product.VerifiedBy = verifiedBy;
        product.VerifiedAt = DateTime.UtcNow;
        product.Status = ProductState.Verified;
        product.UpdatedBy = verifiedBy;
        product.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Product>().Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        _cache.Remove(cacheKey);

        _logger.LogInformation("Verified barcode product: {Barcode} by {User}", barcode, verifiedBy);

        return true;
    }

    public async Task<IEnumerable<BarcodeProductInfo>> SearchAsync(
        string searchTerm,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Enumerable.Empty<BarcodeProductInfo>();
        }

        var normalized = searchTerm.Trim().ToLower();
        var products = await _unitOfWork.Repository<Product>().FindAsync(p =>
            p.Status != ProductState.Hidden && (
                p.Barcode.Contains(searchTerm) ||
                p.Name.ToLower().Contains(normalized)));

        var takeProducts = products.Take(limit).ToList();
        var result = new List<BarcodeProductInfo>(takeProducts.Count);

        foreach (var product in takeProducts)
        {
            var detail = await _unitOfWork.Repository<ProductDetail>()
                .FirstOrDefaultAsync(d => d.ProductId == product.ProductId);
            var category = product.CategoryId.HasValue
                ? await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.CategoryId == product.CategoryId.Value)
                : null;
            result.Add(MapToProductInfo(product, detail, category, "database"));
        }

        return result;
    }

    public async Task<IEnumerable<BarcodeProductInfo>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => !p.VerifiedAt.HasValue || p.Status == ProductState.Draft);

        var result = new List<BarcodeProductInfo>();
        foreach (var product in products)
        {
            var detail = await _unitOfWork.Repository<ProductDetail>()
                .FirstOrDefaultAsync(d => d.ProductId == product.ProductId);
            var category = product.CategoryId.HasValue
                ? await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.CategoryId == product.CategoryId.Value)
                : null;
            result.Add(MapToProductInfo(product, detail, category, "database"));
        }

        return result;
    }

    #region Private Methods

    private async Task SaveToDatabase(BarcodeProductInfo productInfo, CancellationToken cancellationToken)
    {
        try
        {
            await SaveProductAsync(productInfo, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save barcode {Barcode} to database", productInfo.Barcode);
            // Don't throw - lookup still succeeded
        }
    }

    private async Task<Category?> ResolveCategoryAsync(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        var normalized = categoryName.Trim().ToLower();
        var categories = await _unitOfWork.Repository<Category>()
            .FindAsync(c => c.IsActive && c.Name.ToLower() == normalized);

        return categories.FirstOrDefault();
    }

    private BarcodeProductInfo MapToProductInfo(Product product, ProductDetail? detail, Category? category, string source)
    {
        Dictionary<string, string>? nutritionFacts = null;
        if (!string.IsNullOrWhiteSpace(detail?.NutritionFacts))
        {
            try
            {
                nutritionFacts = JsonSerializer.Deserialize<Dictionary<string, string>>(detail.NutritionFacts);
            }
            catch
            {
                nutritionFacts = null;
            }
        }

        return new BarcodeProductInfo
        {
            Barcode = product.Barcode,
            ProductName = product.Name,
            Brand = detail?.Brand,
            Category = category?.Name,
            Description = detail?.Description,
            Manufacturer = detail?.Manufacturer,
            Ingredients = detail?.Ingredients,
            NutritionFacts = nutritionFacts,
            Country = detail?.CountryOfOrigin,
            Gs1Prefix = GetGs1Prefix(product.Barcode),
            IsVietnameseProduct = IsVietnameseBarcode(product.Barcode),
            Source = source,
            Confidence = 1.0f,
            LookupTimestamp = product.UpdatedAt,
            ScanCount = 0,
            IsVerified = product.VerifiedAt.HasValue
        };
    }

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






