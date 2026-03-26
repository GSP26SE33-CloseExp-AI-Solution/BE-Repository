using Amazon.S3.Model;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IR2StorageService
{
    Task<ProductImage> UploadProductImageToR2Async(Stream fileStream, string fileName, string contentType, Guid productId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<List<S3Object>> GetAllFilesAsync(CancellationToken cancellationToken = default);
    Task<object> UploadToR2Async(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    string GeneratePreSignedUrl(string key, TimeSpan expiry);
    string? GetPreSignedUrlForImage(string imageUrl, TimeSpan? expiry = null);
}
