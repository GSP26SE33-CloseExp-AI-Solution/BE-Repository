using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Service quản lý ảnh người dùng (avatar, cover...)
/// </summary>
public interface IUserImageService
{
    /// <summary>Upload ảnh người dùng lên R2</summary>
    Task<UserImage> UploadAsync(Stream fileStream, string fileName, string contentType, Guid userId,
        string imageType = "avatar", bool isPrimary = false, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả ảnh của user</summary>
    Task<IEnumerable<UserImage>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Lấy ảnh đại diện chính</summary>
    Task<UserImage?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Đặt ảnh làm avatar chính</summary>
    Task<bool> SetPrimaryAsync(Guid userId, Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>Xóa ảnh (cả R2 và DB)</summary>
    Task<bool> DeleteAsync(Guid imageId, CancellationToken cancellationToken = default);
}
