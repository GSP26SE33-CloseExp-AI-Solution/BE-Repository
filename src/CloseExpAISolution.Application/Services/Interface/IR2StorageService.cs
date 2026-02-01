using Amazon.S3.Model;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IR2StorageService
{
    Task<ProductImage> UploadProductImageToR2Async(Stream fileStream, string fileName, string contentType, Guid productId, CancellationToken cancellationToken = default);
    Task<List<S3Object>> GetAllFilesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<object> UploadToR2Async(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    string GeneratePreSignedUrl(string key, TimeSpan expiry);
    string? GetPreSignedUrlForImage(string imageUrl, TimeSpan? expiry = null);

    // User Images
    Task<UserImage> UploadUserImageAsync(Stream fileStream, string fileName, string contentType, Guid userId, string imageType = "avatar", bool isPrimary = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserImage>> GetImagesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserImage?> GetPrimaryUserImageAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> SetPrimaryUserImageAsync(Guid userId, Guid imageId, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserImageAsync(Guid imageId, CancellationToken cancellationToken = default);
}