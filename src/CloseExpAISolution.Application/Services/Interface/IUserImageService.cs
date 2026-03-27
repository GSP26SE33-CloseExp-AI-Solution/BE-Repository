using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IUserImageService
{
    Task<UserImage> UploadAsync(Stream fileStream, string fileName, string contentType, Guid userId,
        string imageType = "avatar", bool IsPrimary = false, CancellationToken cancellationToken = default);

    Task<IEnumerable<UserImage>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserImage?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> SetPrimaryAsync(Guid userId, Guid imageId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid imageId, CancellationToken cancellationToken = default);
}