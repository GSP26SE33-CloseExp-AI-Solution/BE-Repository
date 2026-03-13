using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IAIProductService
{
    Task<ProductExtractionResult> ExtractProductInfoAsync(
        Guid productId,
        string imageUrl,
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default);
    Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
    Task<PricingSuggestionResult> GetPriceSuggestionAsync(
        string category,
        DateTime expiryDate,
        decimal originalPrice,
        string? brand = null,
        CancellationToken cancellationToken = default);
    Task<ShelfAnalysisResult> AnalyzeShelfImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);
    Task<ProductProcessingResult> ProcessProductAsync(
        Guid productId,
        string imageUrl,
        CancellationToken cancellationToken = default);
    Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
    Task<SmartScanResult> SmartScanAsync(
        string imageUrl,
        string productTypeHint = "auto",
        bool lookupBarcode = true,
        CancellationToken cancellationToken = default);
    Task<FreshProduceResult> IdentifyFreshProduceAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);
}

