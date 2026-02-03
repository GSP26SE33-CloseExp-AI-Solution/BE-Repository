using CloseExpAISolution.Application.AIService.Models;

namespace CloseExpAISolution.Application.AIService.Interfaces;

/// <summary>
/// Interface for AI Service client operations
/// </summary>
public interface IAIServiceClient
{
    #region Health Checks

    /// <summary>
    /// Check if AI service is healthy
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if AI service is ready (models loaded)
    /// </summary>
    Task<ReadyResponse?> CheckReadinessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI service information
    /// </summary>
    Task<ServiceInfoResponse?> GetServiceInfoAsync(CancellationToken cancellationToken = default);

    #endregion

    #region OCR Operations

    /// <summary>
    /// Extract product information from image using OCR
    /// </summary>
    /// <param name="imageUrl">URL of the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR extraction result</returns>
    Task<OcrResponse?> ExtractFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract product information from base64 image using OCR
    /// </summary>
    /// <param name="imageBase64">Base64 encoded image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR extraction result</returns>
    Task<OcrResponse?> ExtractFromBase64Async(string imageBase64, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract product information with custom options
    /// </summary>
    /// <param name="request">OCR request with options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>OCR extraction result</returns>
    Task<OcrResponse?> ExtractAsync(OcrRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Pricing Operations

    /// <summary>
    /// Get price suggestion for a product
    /// </summary>
    /// <param name="request">Pricing request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing suggestion</returns>
    Task<PricingResponse?> GetPriceSuggestionAsync(PricingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get price suggestion with basic parameters
    /// </summary>
    /// <param name="productType">Product category</param>
    /// <param name="daysToExpire">Days until expiration</param>
    /// <param name="basePrice">Original price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing suggestion</returns>
    Task<PricingResponse?> GetPriceSuggestionAsync(
        string productType, 
        int daysToExpire, 
        decimal basePrice, 
        CancellationToken cancellationToken = default);

    #endregion

    #region Vision Operations

    /// <summary>
    /// Analyze image for product detection
    /// </summary>
    /// <param name="request">Vision analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vision analysis result</returns>
    Task<VisionResponse?> AnalyzeImageAsync(VisionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze image from URL
    /// </summary>
    /// <param name="imageUrl">URL of the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vision analysis result</returns>
    Task<VisionResponse?> AnalyzeImageFromUrlAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get annotated image as bytes
    /// </summary>
    /// <param name="imageUrl">URL of the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Annotated image bytes</returns>
    Task<byte[]?> GetAnnotatedImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Extended interface for batch operations
/// </summary>
public interface IAIServiceBatchClient : IAIServiceClient
{
    /// <summary>
    /// Process multiple images for OCR in parallel
    /// </summary>
    Task<IEnumerable<OcrResponse?>> ExtractBatchAsync(
        IEnumerable<string> imageUrls, 
        int maxConcurrency = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get price suggestions for multiple products
    /// </summary>
    Task<IEnumerable<PricingResponse?>> GetPriceSuggestionsBatchAsync(
        IEnumerable<PricingRequest> requests,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default);
}
