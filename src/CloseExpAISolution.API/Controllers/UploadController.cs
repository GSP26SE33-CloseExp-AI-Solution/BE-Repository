using Amazon.S3;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/bmp"
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"
    };

    private readonly IServiceProviders _services;

    public UploadController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<ApiResponse<ProductImage>>> Upload(
        IFormFile file,
        [FromForm] Guid productId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<ProductImage>.ErrorResponse("No file provided"));

        if (productId == Guid.Empty)
            return BadRequest(ApiResponse<ProductImage>.ErrorResponse("ProductId is required"));

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageContentTypes.Contains(file.ContentType) || !AllowedImageExtensions.Contains(extension))
            return BadRequest(ApiResponse<ProductImage>.ErrorResponse("Only image files are allowed (JPEG, PNG, GIF, WebP, BMP)"));

        try
        {
            await using var stream = file.OpenReadStream();
            var productImage = await _services.R2StorageService.UploadProductImageToR2Async(
                stream,
                file.FileName,
                file.ContentType,
                productId,
                cancellationToken);
            return Ok(ApiResponse<ProductImage>.SuccessResponse(productImage, "Image uploaded successfully"));
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode(502, ApiResponse<ProductImage>.ErrorResponse($"R2 upload failed: {ex.Message}. Check R2 credentials and bucket permissions."));
        }
    }

    /// <summary>
    /// Test upload to R2 - uploads any file and returns Key and Url (no ProductImage record).
    /// </summary>
    [HttpPost("test")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<ActionResult<ApiResponse<object>>> UploadTest(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("No file provided"));

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _services.R2StorageService.UploadToR2Async(
                stream,
                file.FileName,
                file.ContentType,
                cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(result, "File uploaded to R2 successfully"));
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode(502, ApiResponse<object>.ErrorResponse($"R2 upload failed: {ex.Message}. Check R2 credentials and bucket permissions."));
        }
    }

    [HttpGet("files")]
    public async Task<ActionResult<ApiResponse<object>>> GetAllFiles(CancellationToken cancellationToken)
    {
        try
        {
            var objects = await _services.R2StorageService.GetAllFilesAsync(cancellationToken);
            var result = objects.Select(o => new { o.Key, o.Size, o.LastModified }).ToList();
            return Ok(ApiResponse<object>.SuccessResponse(result!));
        }
        catch (AmazonS3Exception ex)
        {
            return StatusCode(502, ApiResponse<object>.ErrorResponse($"R2 error: {ex.Message}. Check R2 credentials and bucket permissions."));
        }
    }

    [HttpGet("product/{productId:guid}/images")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductImage>>>> GetImagesByProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var images = await _services.R2StorageService.GetImagesByProductIdAsync(productId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<ProductImage>>.SuccessResponse(images));
    }

    /// <summary>
    /// Get a pre-signed URL for viewing an image. Use this URL in browser or img src.
    /// </summary>
    [HttpGet("image/{productImageId:guid}/presign")]
    public async Task<ActionResult<ApiResponse<object>>> GetPresignedUrl(
        Guid productImageId,
        [FromQuery] int expiryMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        var productImage = await _services.ProductImageService.FirstOrDefaultAsync(pi => pi.ProductImageId == productImageId);
        if (productImage == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Product image not found"));

        var presignedUrl = _services.R2StorageService.GetPreSignedUrlForImage(
            productImage.ImageUrl,
            TimeSpan.FromMinutes(Math.Clamp(expiryMinutes, 1, 10080))); // max 7 days

        if (string.IsNullOrEmpty(presignedUrl))
            return BadRequest(ApiResponse<object>.ErrorResponse("Could not generate presigned URL for this image"));

        return Ok(ApiResponse<object>.SuccessResponse(new { PresignedUrl = presignedUrl }));
    }

    /// <summary>
    /// Get a pre-signed URL by R2 object key (e.g. from test upload response).
    /// </summary>
    [HttpGet("presign")]
    public ActionResult<ApiResponse<object>> GetPresignedUrlByKey([FromQuery] string key, [FromQuery] int expiryMinutes = 60)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(ApiResponse<object>.ErrorResponse("Key is required"));

        var presignedUrl = _services.R2StorageService.GeneratePreSignedUrl(
            key,
            TimeSpan.FromMinutes(Math.Clamp(expiryMinutes, 1, 10080)));

        return Ok(ApiResponse<object>.SuccessResponse(new { PresignedUrl = presignedUrl }));
    }
}
