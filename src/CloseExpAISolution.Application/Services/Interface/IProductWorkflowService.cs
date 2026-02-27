using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Service interface for Product Workflow operations.
/// Handles the complete product lifecycle:
/// - Existing Product: Scan Barcode → Create ProductLot → Pricing → Publish
/// - New Product: Scan Barcode → Upload OCR → Verify → Create Product + ProductLot → Pricing → Publish
/// </summary>
public interface IProductWorkflowService
{
    #region Step 1: Scan Barcode (Check Product Exists)

    /// <summary>
    /// Scan barcode and check if product already exists in database.
    /// Returns existing product info or barcode lookup info for new products.
    /// </summary>
    /// <param name="barcode">Barcode to scan</param>
    /// <param name="supermarketId">Supermarket ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scan result with next action guidance</returns>
    Task<ScanBarcodeResponseDto> ScanBarcodeAsync(
        string barcode,
        Guid supermarketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 2a: Create ProductLot from Existing Product

    /// <summary>
    /// Create a new ProductLot from existing Product (product already in database).
    /// REQUIRES: Product must be in Verified status.
    /// This skips OCR analysis since product info already exists.
    /// </summary>
    Task<ProductLotResponseDto> CreateProductLotFromExistingAsync(
        CreateProductLotFromExistingDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 2b: OCR Analysis for New Product

    /// <summary>
    /// Upload image and extract info using AI OCR (for new products only).
    /// Does NOT create product yet - just returns extracted info for user to verify.
    /// </summary>
    Task<OcrAnalysisResponseDto> AnalyzeProductImageAsync(
        Guid supermarketId,
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create new Product (Draft) from OCR info + user input.
    /// Does NOT create ProductLot - user must verify Product first, then create ProductLot separately.
    /// </summary>
    Task<CreateNewProductResponseDto> CreateNewProductAsync(
        CreateNewProductRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// [DEPRECATED] Create new Product and ProductLot at the same time.
    /// Use CreateNewProductAsync + VerifyProductAsync + CreateProductLotFromExistingAsync instead.
    /// </summary>
    [Obsolete("Use CreateNewProductAsync + VerifyProductAsync + CreateProductLotFromExistingAsync instead")]
    Task<ProductLotResponseDto> CreateNewProductAndLotAsync(
        CreateNewProductRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Legacy Upload & OCR (Deprecated - use ScanBarcodeAsync flow instead)

    /// <summary>
    /// [DEPRECATED] Upload product image, extract info using AI OCR, and create draft product.
    /// Use ScanBarcodeAsync flow instead.
    /// </summary>
    [Obsolete("Use ScanBarcodeAsync + AnalyzeProductImageAsync + CreateNewProductAndLotAsync instead")]
    Task<ProductResponseDto> UploadAndExtractAsync(
        Guid supermarketId,
        string createdBy,
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 3: Verify Product (for legacy flow)

    /// <summary>
    /// Verify a draft product - confirm/correct OCR extracted info.
    /// Changes status from Draft to Verified.
    /// </summary>
    Task<ProductResponseDto> VerifyProductAsync(
        Guid productId,
        VerifyProductRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 4: Get Pricing Suggestion

    /// <summary>
    /// Get AI pricing suggestion for a ProductLot.
    /// </summary>
    Task<PricingSuggestionResponseDto> GetLotPricingSuggestionAsync(
        Guid lotId,
        GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get AI pricing suggestion for a verified product (legacy).
    /// </summary>
    Task<PricingSuggestionResponseDto> GetPricingSuggestionAsync(
        Guid productId,
        GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 5: Confirm Price

    /// <summary>
    /// Confirm the final price for a ProductLot and change status to PRICED.
    /// </summary>
    Task<ProductLotResponseDto> ConfirmLotPriceAsync(
        Guid lotId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirm the final price and change status to PRICED (legacy).
    /// </summary>
    Task<ProductResponseDto> ConfirmPriceAsync(
        Guid productId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Step 6: Publish ProductLot

    /// <summary>
    /// Publish a priced ProductLot to make it visible to customers.
    /// </summary>
    Task<ProductLotResponseDto> PublishProductLotAsync(
        Guid lotId,
        PublishProductRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a priced product (legacy).
    /// </summary>
    Task<ProductResponseDto> PublishProductAsync(
        Guid productId,
        PublishProductRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Query Methods

    /// <summary>
    /// Get a product by ID.
    /// </summary>
    Task<ProductResponseDto?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a ProductLot by ID.
    /// </summary>
    Task<ProductLotResponseDto?> GetProductLotAsync(
        Guid lotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get products by status for a supermarket.
    /// </summary>
    Task<IEnumerable<ProductResponseDto>> GetProductsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get ProductLots by status for a supermarket.
    /// </summary>
    Task<IEnumerable<ProductLotResponseDto>> GetProductLotsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get workflow summary (count of lots in each status).
    /// </summary>
    Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(
        Guid supermarketId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Quick Actions

    /// <summary>
    /// Quick approve: Verify + Confirm Price + Publish in one step.
    /// </summary>
    Task<ProductResponseDto> QuickApproveAsync(
        Guid productId,
        QuickApproveRequestDto request,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Summary of products in each workflow state
/// </summary>
public class WorkflowSummaryDto
{
    public int DraftCount { get; set; }
    public int VerifiedCount { get; set; }
    public int PricedCount { get; set; }
    public int PublishedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Request for quick approve
/// </summary>
public class QuickApproveRequestDto
{
    public decimal OriginalPrice { get; set; }
    public decimal? FinalPrice { get; set; }
    public string StaffId { get; set; } = string.Empty;
    public bool AcceptAiSuggestion { get; set; } = true;
}
