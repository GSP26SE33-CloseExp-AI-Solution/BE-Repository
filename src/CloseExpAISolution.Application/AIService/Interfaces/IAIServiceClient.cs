using CloseExpAISolution.Application.AIService.Models;

namespace CloseExpAISolution.Application.AIService.Interfaces;

public interface IAIServiceClient
{
    #region Health Checks

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    Task<ReadyResponse?> CheckReadinessAsync(CancellationToken cancellationToken = default);
    Task<ServiceInfoResponse?> GetServiceInfoAsync(CancellationToken cancellationToken = default);

    #endregion

    #region OCR Operations
    Task<OcrResponse?> ExtractFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task<OcrResponse?> ExtractFromBase64Async(string imageBase64, CancellationToken cancellationToken = default);
    Task<OcrResponse?> ExtractAsync(OcrRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Pricing Operations
    Task<PricingResponse?> GetPriceSuggestionAsync(PricingRequest request, CancellationToken cancellationToken = default);
    Task<PricingResponse?> GetPriceSuggestionAsync(
        string productType,
        int daysToExpire,
        decimal basePrice,
        CancellationToken cancellationToken = default);

    #endregion

    #region Vision Operations
    Task<VisionResponse?> AnalyzeImageAsync(VisionRequest request, CancellationToken cancellationToken = default);
    Task<VisionResponse?> AnalyzeImageFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default);
    Task<byte[]?> GetAnnotatedImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    #endregion

    #region Fresh Produce Operations
    Task<FreshProduceResponse?> IdentifyFreshProduceAsync(FreshProduceRequest request, CancellationToken cancellationToken = default);
    Task<FreshProduceResponse?> IdentifyFreshProduceFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default);

    #endregion

    #region Smart Scan Operations
    Task<SmartScanResponse> SmartScanAsync(SmartScanRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Market Price Operations
    Task<MarketPriceCrawlResponse?> CrawlMarketPricesAsync(
        string barcode,
        string? productName = null,
        CancellationToken cancellationToken = default);

    #endregion
}

public interface IAIServiceBatchClient : IAIServiceClient
{
    Task<IEnumerable<OcrResponse?>> ExtractBatchAsync(
        IEnumerable<string> imageUrls,
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<PricingResponse?>> GetPriceSuggestionsBatchAsync(
        IEnumerable<PricingRequest> requests,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default);
}
