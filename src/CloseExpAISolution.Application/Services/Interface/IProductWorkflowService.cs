using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Service interface for Product Workflow operations.
/// Handles the complete product lifecycle: Upload → OCR → Verify → Price → Publish
/// </summary>
public interface IProductWorkflowService
{
    #region Step 1: Upload & OCR
    
    /// <summary>
    /// Upload product image, extract info using AI OCR, and create draft product.
    /// </summary>
    /// <param name="supermarketId">Supermarket ID</param>
    /// <param name="createdBy">Staff ID who uploaded</param>
    /// <param name="imageStream">Image stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">Image content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created draft product with OCR extracted info</returns>
    Task<ProductResponseDto> UploadAndExtractAsync(
        Guid supermarketId,
        string createdBy,
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Step 2: Verify Product
    
    /// <summary>
    /// Verify a draft product - confirm/correct OCR extracted info.
    /// Changes status from Draft to Verified.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Verification data with corrections</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verified product info</returns>
    Task<ProductResponseDto> VerifyProductAsync(
        Guid productId,
        VerifyProductRequestDto request,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Step 3: Get Pricing Suggestion
    
    /// <summary>
    /// Get AI pricing suggestion for a verified product.
    /// Sets the original price and returns pricing recommendation.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Request with original price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pricing suggestion with market comparison</returns>
    Task<PricingSuggestionResponseDto> GetPricingSuggestionAsync(
        Guid productId,
        GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Step 4: Confirm Price
    
    /// <summary>
    /// Confirm the final price and change status to PRICED.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Price confirmation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Priced product</returns>
    Task<ProductResponseDto> ConfirmPriceAsync(
        Guid productId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Step 5: Publish Product
    
    /// <summary>
    /// Publish a priced product to make it visible to customers.
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="request">Publish request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Published product</returns>
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
    /// Get products by status for a supermarket.
    /// </summary>
    Task<IEnumerable<ProductResponseDto>> GetProductsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get workflow summary (count of products in each status).
    /// </summary>
    Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(
        Guid supermarketId,
        CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Quick Actions
    
    /// <summary>
    /// Quick approve: Verify + Confirm Price + Publish in one step.
    /// For trusted staff who want to speed up the workflow.
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
