using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services;

public class AIProductService : IAIProductService
{
    private readonly IAIServiceClient _aiServiceClient;
    private readonly IBarcodeLookupService _barcodeLookupService;
    private readonly ILogger<AIProductService> _logger;
    private readonly HttpClient _httpClient;

    public AIProductService(
        IAIServiceClient aiServiceClient,
        IBarcodeLookupService barcodeLookupService,
        ILogger<AIProductService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _aiServiceClient = aiServiceClient;
        _barcodeLookupService = barcodeLookupService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ImageDownloader");
    }

    public async Task<ProductExtractionResult> ExtractProductInfoAsync(
        Guid productId,
        string imageUrl,
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting product info for Product {ProductId} from {ImageUrl}",
            productId, imageUrl);

        try
        {
            // Download image and convert to base64 to avoid CDN hotlink protection
            string? imageBase64 = null;
            try
            {
                imageBase64 = await DownloadImageAsBase64Async(imageUrl, cancellationToken);
                _logger.LogDebug("Successfully downloaded image as base64, size: {Size} bytes",
                    imageBase64?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image from {ImageUrl}, falling back to URL mode", imageUrl);
            }

            var request = new OcrRequest
            {
                ImageUrl = imageBase64 == null ? imageUrl : null,
                ImageB64 = imageBase64,
                ExtractDates = true,
                ExtractBarcode = true,
                Languages = new List<string> { "vi", "en" }
            };

            var response = await _aiServiceClient.ExtractAsync(request, cancellationToken);

            if (response == null)
            {
                return new ProductExtractionResult
                {
                    Success = false,
                    ErrorMessage = "No response from AI service"
                };
            }

            var result = new ProductExtractionResult
            {
                Success = true,
                Name = response.ProductInfo?.Name ?? response.Name,
                Brand = response.ProductInfo?.Brand ?? response.Brand,
                Barcode = response.ProductInfo?.Barcode ?? response.Barcode,
                ExpiryDate = response.ExpiryDate?.Value,
                ManufacturedDate = response.ManufacturedDate?.Value,
                OverallConfidence = response.Confidence,
                ExpiryDateConfidence = response.ExpiryDate?.Confidence,
                ManufacturedDateConfidence = response.ManufacturedDate?.Confidence,
                ProcessingTimeMs = response.ProcessingTimeMs ?? 0,
                Warnings = response.Warnings ?? new List<string>()
            };

            // If barcode was extracted and lookup is enabled, enrich with product info
            if (lookupBarcode && !string.IsNullOrEmpty(result.Barcode))
            {
                _logger.LogInformation("Looking up barcode {Barcode} for additional product info", result.Barcode);

                try
                {
                    var barcodeInfo = await _barcodeLookupService.LookupAsync(result.Barcode, cancellationToken);

                    if (barcodeInfo != null)
                    {
                        result.BarcodeInfo = barcodeInfo;
                        _logger.LogInformation(
                            "Barcode lookup successful. Product: {ProductName}, Brand: {Brand}, Source: {Source}",
                            barcodeInfo.ProductName, barcodeInfo.Brand, barcodeInfo.Source);
                    }
                    else
                    {
                        result.Warnings.Add($"Barcode {result.Barcode} not found in product database");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to lookup barcode {Barcode}", result.Barcode);
                    result.Warnings.Add($"Barcode lookup failed: {ex.Message}");
                }
            }

            _logger.LogInformation(
                "Product extraction completed for {ProductId}. Success: {Success}, Confidence: {Confidence}, BarcodeFound: {BarcodeFound}",
                productId, result.Success, result.OverallConfidence, result.BarcodeFound);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting product info for Product {ProductId}", productId);
            return new ProductExtractionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("Getting price suggestion for Product {ProductId}", productId);

        // In a real implementation, you would fetch product details from repository
        // For now, this method signature is provided for future integration
        throw new NotImplementedException(
            "This method requires product repository integration. Use the overload with explicit parameters.");
    }

    public async Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        string category,
        DateTime expiryDate,
        decimal originalPrice,
        string? brand = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting price suggestion for Category: {Category}, Expiry: {ExpiryDate}, Original: {OriginalPrice}",
            category, expiryDate, originalPrice);

        try
        {
            var daysToExpire = Math.Max(0, (int)(expiryDate - DateTime.UtcNow).TotalDays);

            var request = new PricingRequest
            {
                ProductType = category,
                DaysToExpire = daysToExpire,
                BasePrice = originalPrice,
                ExpiryDate = expiryDate,
                Brand = brand,
                Strategy = "balanced"
            };

            var response = await _aiServiceClient.GetPriceSuggestionAsync(request, cancellationToken);

            if (response == null)
            {
                return new PricingSuggestionResult
                {
                    Success = false,
                    ErrorMessage = "No response from AI service",
                    OriginalPrice = originalPrice,
                    Category = category
                };
            }

            var result = new PricingSuggestionResult
            {
                Success = true,
                SuggestedPrice = response.SuggestedPrice,
                MinPrice = response.MinSuggestedPrice,
                MaxPrice = response.MaxSuggestedPrice,
                DiscountPercent = response.DiscountPercent,
                Confidence = response.Confidence,
                ExpectedSellRate = response.ExpectedSellRate,
                EstimatedTimeToSell = response.EstimatedTimeToSell,
                Competitiveness = response.Competitiveness,
                Reasons = response.Reasons ?? new List<string>(),
                UrgencyLevel = response.UrgencyLevel,
                RecommendedAction = response.RecommendedAction,
                DaysToExpire = daysToExpire,
                OriginalPrice = originalPrice,
                Category = category,
                ProcessingTimeMs = response.CalculationTimeMs ?? 0
            };

            _logger.LogInformation(
                "Price suggestion completed. Suggested: {SuggestedPrice}, Discount: {DiscountPercent}%",
                result.SuggestedPrice, result.DiscountPercent);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price suggestion for category {Category}", category);
            return new PricingSuggestionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                OriginalPrice = originalPrice,
                Category = category
            };
        }
    }


    public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _aiServiceClient.IsHealthyAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    #region Private Helper Methods

    private async Task<string> DownloadImageAsBase64Async(string imageUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);

        // Add headers to mimic browser request (bypass CDN protection)
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        request.Headers.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
        request.Headers.Add("Referer", new Uri(imageUrl).GetLeftPart(UriPartial.Authority) + "/");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return Convert.ToBase64String(imageBytes);
    }

    #endregion
}

