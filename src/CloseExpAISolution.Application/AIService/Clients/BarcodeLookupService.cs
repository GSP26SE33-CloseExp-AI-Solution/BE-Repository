using CloseExpAISolution.Application.AIService.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloseExpAISolution.Application.AIService.Clients;

/// <summary>
/// Service for looking up product information from barcode using Open Food Facts API
/// With caching to reduce API calls
/// </summary>
public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BarcodeLookupService> _logger;
    
    private const string OPEN_FOOD_FACTS_API = "https://world.openfoodfacts.org/api/v2/product/";
    private const string CACHE_PREFIX = "barcode_";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7); // Cache for 7 days

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public BarcodeLookupService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<BarcodeLookupService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("BarcodeLookup");
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<BarcodeProductInfo?> LookupAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        // Normalize barcode (remove spaces, dashes)
        barcode = NormalizeBarcode(barcode);
        
        // Check cache first
        var cacheKey = $"{CACHE_PREFIX}{barcode}";
        if (_cache.TryGetValue(cacheKey, out BarcodeProductInfo? cachedResult))
        {
            _logger.LogDebug("Barcode {Barcode} found in cache", barcode);
            return cachedResult;
        }

        _logger.LogInformation("Looking up barcode {Barcode} from Open Food Facts", barcode);

        try
        {
            // Try Open Food Facts first (free, open-source, has Vietnamese products)
            var result = await LookupFromOpenFoodFactsAsync(barcode, cancellationToken);
            
            if (result != null)
            {
                // Cache the result
                _cache.Set(cacheKey, result, CacheDuration);
                _logger.LogInformation("Found product for barcode {Barcode}: {ProductName}", 
                    barcode, result.ProductName);
                return result;
            }

            // Cache null result to avoid repeated lookups for unknown barcodes
            _cache.Set(cacheKey, (BarcodeProductInfo?)null, TimeSpan.FromHours(24));
            _logger.LogWarning("No product found for barcode {Barcode}", barcode);
            return null;
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
        var tasks = barcodes.Select(async barcode =>
        {
            var result = await LookupAsync(barcode, cancellationToken);
            return (barcode, result);
        });

        var lookupResults = await Task.WhenAll(tasks);
        
        foreach (var (barcode, result) in lookupResults)
        {
            results[barcode] = result;
        }

        return results;
    }

    #region Private Methods

    private async Task<BarcodeProductInfo?> LookupFromOpenFoodFactsAsync(
        string barcode, 
        CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{OPEN_FOOD_FACTS_API}{barcode}.json";
            
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "CloseExpAI/1.0 (contact@closeexp.com)");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Open Food Facts returned {StatusCode} for barcode {Barcode}", 
                    response.StatusCode, barcode);
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
                Source = "openfoodfacts",
                Confidence = 0.9f,
                LookupTimestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to lookup barcode {Barcode} from Open Food Facts", barcode);
            return null;
        }
    }

    private static string NormalizeBarcode(string barcode)
    {
        return new string(barcode.Where(char.IsDigit).ToArray());
    }

    private static string? GetBestProductName(OpenFoodFactsProduct product)
    {
        // Prefer Vietnamese name, then English, then generic
        return product.ProductNameVi 
            ?? product.ProductName 
            ?? product.ProductNameEn 
            ?? product.GenericNameVi 
            ?? product.GenericName;
    }

    private static string? GetBestCategory(OpenFoodFactsProduct product)
    {
        // Get the most specific category
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

#region Open Food Facts API Models

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

#endregion
