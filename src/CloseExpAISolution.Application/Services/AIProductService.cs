using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services;

/// <summary>
/// Implementation of AI-powered product service
/// </summary>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
                // New fields
                ExpectedSellRate = response.ExpectedSellRate,
                EstimatedTimeToSell = response.EstimatedTimeToSell,
                Competitiveness = response.Competitiveness,
                Reasons = response.Reasons ?? new List<string>(),
                // Existing fields
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

    /// <inheritdoc/>
    public async Task<ShelfAnalysisResult> AnalyzeShelfImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing shelf image: {ImageUrl}", imageUrl);

        try
        {
            var request = new VisionRequest
            {
                ImageUrl = imageUrl,
                MinConfidence = 0.5f,
                ReturnAnnotatedImage = true,
                AssessQuality = true
            };

            var response = await _aiServiceClient.AnalyzeImageAsync(request, cancellationToken);

            if (response == null)
            {
                return new ShelfAnalysisResult
                {
                    Success = false,
                    ErrorMessage = "No response from AI service"
                };
            }

            var result = new ShelfAnalysisResult
            {
                Success = true,
                TotalProducts = response.DetectionCount,
                ImageQuality = response.ImageQuality?.Label ?? "unknown",
                ImageQualityScore = response.ImageQuality?.Score ?? 0,
                AnnotatedImageBase64 = response.AnnotatedImageB64,
                ProcessingTimeMs = response.InferenceTimeMs
            };

            // Map detections
            if (response.Detections != null)
            {
                foreach (var detection in response.Detections)
                {
                    result.Products.Add(new DetectedProduct
                    {
                        Index = detection.Index,
                        ClassName = detection.ClassName,
                        ProductType = detection.ProductType,
                        Confidence = detection.Confidence,
                        BoundingBox = new BoundingBoxResult
                        {
                            X1 = detection.BoundingBox.X1,
                            Y1 = detection.BoundingBox.Y1,
                            X2 = detection.BoundingBox.X2,
                            Y2 = detection.BoundingBox.Y2
                        }
                    });

                    // Build category summary
                    var category = detection.ProductType;
                    if (!result.CategorySummary.ContainsKey(category))
                    {
                        result.CategorySummary[category] = 0;
                    }
                    result.CategorySummary[category]++;
                }
            }

            _logger.LogInformation(
                "Shelf analysis completed. Found {ProductCount} products in {ProcessingTime}ms", 
                result.TotalProducts, result.ProcessingTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing shelf image");
            return new ShelfAnalysisResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ProductProcessingResult> ProcessProductAsync(
        Guid productId,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing product {ProductId} with full AI pipeline", productId);

        var startTime = DateTime.UtcNow;
        var result = new ProductProcessingResult();

        try
        {
            // Step 1: Extract product info via OCR (with barcode lookup enabled)
            var extractionResult = await ExtractProductInfoAsync(productId, imageUrl, lookupBarcode: true, cancellationToken);
            result.Extraction = extractionResult;
            result.ExtractedName = extractionResult.BestName; // Use best name (from barcode if available)
            result.ExtractedExpiryDate = extractionResult.ExpiryDate;

            if (!extractionResult.Success)
            {
                _logger.LogWarning("OCR extraction failed for product {ProductId}", productId);
                result.Success = false;
                result.ErrorMessage = $"OCR extraction failed: {extractionResult.ErrorMessage}";
                return result;
            }

            // Step 2: If we have expiry date, get pricing suggestion
            if (extractionResult.ExpiryDate.HasValue)
            {
                // Use category from barcode lookup if available
                var category = extractionResult.Category ?? "general";
                var brand = extractionResult.BestBrand;
                
                var pricingResult = await GetPriceSuggestionAsync(
                    category: category,
                    expiryDate: extractionResult.ExpiryDate.Value,
                    originalPrice: 0, // Would come from product repository
                    brand: brand,
                    cancellationToken: cancellationToken);

                result.Pricing = pricingResult;
                result.SuggestedPrice = pricingResult.SuggestedPrice;
            }

            // Calculate overall confidence
            result.OverallConfidence = CalculateOverallConfidence(extractionResult, result.Pricing);
            result.Success = true;
            result.TotalProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Product processing completed for {ProductId}. Overall confidence: {Confidence}", 
                productId, result.OverallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product {ProductId}", productId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            result.TotalProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;
            return result;
        }
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<SmartScanResult> SmartScanAsync(
        string imageUrl,
        string productTypeHint = "auto",
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Smart scanning image with hint: {Hint}", productTypeHint);

        var startTime = DateTime.UtcNow;
        var result = new SmartScanResult();

        try
        {
            // Prepare image data
            string? imageBase64 = null;
            string? imageUrlToUse = imageUrl;

            // Check if it's already base64
            if (imageUrl.StartsWith("data:image"))
            {
                imageBase64 = imageUrl;
                imageUrlToUse = null;
            }
            else
            {
                // Download image to avoid CDN issues
                try
                {
                    imageBase64 = await DownloadImageAsBase64Async(imageUrl, cancellationToken);
                    imageUrlToUse = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image, using URL directly");
                }
            }

            // Create smart scan request for AI service
            var smartScanRequest = new AIService.Models.SmartScanRequest
            {
                ImageUrl = imageUrlToUse,
                ImageB64 = imageBase64,
                ProductTypeHint = productTypeHint,
                LookupBarcode = lookupBarcode
            };

            var aiResponse = await _aiServiceClient.SmartScanAsync(smartScanRequest, cancellationToken);

            if (!aiResponse.Success)
            {
                return new SmartScanResult
                {
                    Success = false,
                    ErrorMessage = aiResponse.ErrorMessage,
                    ProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            // Map AI response to result
            result.Success = true;
            result.ScanType = aiResponse.ScanType;
            result.IsVietnameseProduct = aiResponse.IsVietnameseProduct;
            result.Confidence = aiResponse.Confidence;

            // Map Vietnamese barcode info
            if (aiResponse.VietnameseBarcodeInfo != null)
            {
                result.VietnameseBarcodeInfo = new VietnameseBarcodeResult
                {
                    Barcode = aiResponse.VietnameseBarcodeInfo.Barcode,
                    IsVietnamese = aiResponse.VietnameseBarcodeInfo.IsVietnamese,
                    Company = aiResponse.VietnameseBarcodeInfo.Company,
                    Category = aiResponse.VietnameseBarcodeInfo.Category,
                    Prefix = aiResponse.VietnameseBarcodeInfo.Prefix,
                    Note = aiResponse.VietnameseBarcodeInfo.Note
                };
            }

            // Extract data from OCR result
            if (aiResponse.OcrResult != null)
            {
                var productInfo = aiResponse.OcrResult.ProductInfo;
                
                result.ProductName = productInfo?.Name ?? aiResponse.OcrResult.Name;
                result.Brand = productInfo?.Brand ?? aiResponse.OcrResult.Brand;
                result.Barcode = productInfo?.Barcode ?? aiResponse.OcrResult.Barcode;
                result.ExpiryDate = aiResponse.OcrResult.ExpiryDate?.Value;
                result.ManufacturedDate = aiResponse.OcrResult.ManufacturedDate?.Value;
                result.Ingredients = productInfo?.Ingredients;
                result.Weight = productInfo?.Weight;
                result.Origin = productInfo?.Origin;
                result.Certifications = productInfo?.Certifications;
                result.StorageRecommendation = productInfo?.StorageInstructions;
                
                // New fields from enhanced OCR
                result.UsageInstructions = productInfo?.UsageInstructions;
                result.QualityStandards = productInfo?.QualityStandards;
                result.Warnings = productInfo?.Warnings;
                result.NutritionFacts = productInfo?.NutritionFacts;
                result.SuggestedShelfLifeDays ??= productInfo?.ShelfLifeDays;
                
                // Map manufacturer info
                if (productInfo?.Manufacturer != null)
                {
                    result.ManufacturerInfo = new ManufacturerInfoResult
                    {
                        Name = productInfo.Manufacturer.Name,
                        Distributor = productInfo.Manufacturer.Distributor,
                        Address = productInfo.Manufacturer.Address,
                        Contact = productInfo.Manufacturer.Contact
                    };
                }
                
                // Map product codes
                if (productInfo?.ProductCodes != null)
                {
                    result.ProductCodes = new ProductCodesResult
                    {
                        Sku = productInfo.ProductCodes.Sku,
                        Batch = productInfo.ProductCodes.Batch,
                        Msktvsty = productInfo.ProductCodes.Msktvsty
                    };
                }
            }

            // Set category and shelf life
            result.SuggestedCategory = aiResponse.SuggestedCategory;
            result.SuggestedShelfLifeDays = aiResponse.SuggestedShelfLifeDays;
            result.StorageRecommendation ??= aiResponse.StorageRecommendation;

            // Map fresh produce items
            if (aiResponse.FreshProduceResult?.DetectedItems?.Any() == true)
            {
                result.FreshProduceItems = aiResponse.FreshProduceResult.DetectedItems
                    .Select(item => new FreshProduceDetection
                    {
                        Category = item.Category,
                        NameVi = item.NameVi,
                        NameEn = item.NameEn,
                        TypicalShelfLifeDays = item.TypicalShelfLifeDays,
                        StorageRecommendation = item.StorageRecommendation,
                        FreshnessIndicators = item.FreshnessIndicators,
                        Confidence = item.Confidence
                    })
                    .ToList();
            }

            // Enrich with external barcode lookup if enabled
            if (lookupBarcode && !string.IsNullOrEmpty(result.Barcode) && result.ProductName == null)
            {
                try
                {
                    var barcodeInfo = await _barcodeLookupService.LookupAsync(result.Barcode, cancellationToken);
                    if (barcodeInfo != null)
                    {
                        result.ProductName ??= barcodeInfo.ProductName;
                        result.Brand ??= barcodeInfo.Brand;
                        result.SuggestedCategory ??= barcodeInfo.Category;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to lookup barcode externally");
                }
            }

            result.ProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Smart scan completed. Type: {ScanType}, Vietnamese: {IsVn}, Confidence: {Confidence}",
                result.ScanType, result.IsVietnameseProduct, result.Confidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during smart scan");
            return new SmartScanResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    /// <inheritdoc/>
    public async Task<FreshProduceResult> IdentifyFreshProduceAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Identifying fresh produce from image");

        var startTime = DateTime.UtcNow;

        try
        {
            // Prepare image data
            string? imageBase64 = null;
            string? imageUrlToUse = imageUrl;

            if (imageUrl.StartsWith("data:image"))
            {
                imageBase64 = imageUrl;
                imageUrlToUse = null;
            }
            else
            {
                try
                {
                    imageBase64 = await DownloadImageAsBase64Async(imageUrl, cancellationToken);
                    imageUrlToUse = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image, using URL directly");
                }
            }

            var request = new AIService.Models.FreshProduceRequest
            {
                ImageUrl = imageUrlToUse,
                ImageB64 = imageBase64
            };

            var response = await _aiServiceClient.IdentifyFreshProduceAsync(request, cancellationToken);

            if (response == null)
            {
                return new FreshProduceResult
                {
                    Success = false,
                    ErrorMessage = "No response from AI service",
                    ProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }

            var result = new FreshProduceResult
            {
                Success = true,
                ProcessingTimeMs = response.ProcessingTimeMs,
                Warnings = response.Warnings
            };

            // Map detected items
            if (response.DetectedItems?.Any() == true)
            {
                result.DetectedItems = response.DetectedItems
                    .Select(item => new FreshProduceDetection
                    {
                        Category = item.Category,
                        NameVi = item.NameVi,
                        NameEn = item.NameEn,
                        TypicalShelfLifeDays = item.TypicalShelfLifeDays,
                        StorageRecommendation = item.StorageRecommendation,
                        FreshnessIndicators = item.FreshnessIndicators,
                        Confidence = item.Confidence
                    })
                    .ToList();
            }

            _logger.LogInformation(
                "Fresh produce identification completed. Found {Count} items in {Time}ms",
                result.DetectedItems.Count, result.ProcessingTimeMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying fresh produce");
            return new FreshProduceResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTimeMs = (float)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
        }
    }

    #region Private Helper Methods

    private static float CalculateOverallConfidence(
        ProductExtractionResult extraction,
        PricingSuggestionResult? pricing)
    {
        var confidenceScores = new List<float> { extraction.OverallConfidence };

        if (pricing?.Success == true)
        {
            confidenceScores.Add(pricing.Confidence);
        }

        return confidenceScores.Average();
    }

    /// <summary>
    /// Download image from URL and convert to base64
    /// This helps bypass CDN hotlink protection
    /// </summary>
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
