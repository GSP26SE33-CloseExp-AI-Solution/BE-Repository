using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class UserImageService : IUserImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IR2StorageService _r2Service;

    public UserImageService(IUnitOfWork unitOfWork, IR2StorageService r2Service)
    {
        _unitOfWork = unitOfWork;
        _r2Service = r2Service;
    }

    public async Task<UserImage> UploadAsync(
        Stream fileStream, string fileName, string contentType, Guid userId,
        string imageType = "avatar", bool IsPrimary = false,
        CancellationToken cancellationToken = default)
    {
        var key = $"users/{userId}/{imageType}";
        var result = await _r2Service.UploadToR2Async(fileStream, fileName, contentType, cancellationToken);
        var imageUrl = ((dynamic)result).Url;

        if (IsPrimary)
            await UnsetPrimaryImages(userId);

        var userImage = new UserImage
        {
            ImageId = Guid.NewGuid(),
            UserId = userId,
            ImageUrl = imageUrl,
            ImageType = imageType,
            IsPrimary = IsPrimary,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<UserImage>().AddAsync(userImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return userImage;
    }

    public async Task<IEnumerable<UserImage>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<UserImage>().FindAsync(ui => ui.UserId == userId);

    public async Task<UserImage?> GetPrimaryAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<UserImage>().FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IsPrimary);

    public async Task<bool> SetPrimaryAsync(Guid userId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var targetImage = await _unitOfWork.Repository<UserImage>()
            .FirstOrDefaultAsync(ui => ui.ImageId == imageId && ui.UserId == userId);

        if (targetImage == null) return false;

        await UnsetPrimaryImages(userId);

        targetImage.IsPrimary = true;
        _unitOfWork.Repository<UserImage>().Update(targetImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _unitOfWork.Repository<UserImage>().FirstOrDefaultAsync(ui => ui.ImageId == imageId);
        if (image == null) return false;

        _unitOfWork.Repository<UserImage>().Delete(image);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    #region Private Helpers

    private async Task UnsetPrimaryImages(Guid userId)
    {
        var images = await _unitOfWork.Repository<UserImage>().FindAsync(ui => ui.UserId == userId && ui.IsPrimary);
        foreach (var img in images)
        {
            img.IsPrimary = false;
            _unitOfWork.Repository<UserImage>().Update(img);
        }
    }

    #endregion
}

