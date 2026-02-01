using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Configuration;

namespace CloseExpAISolution.Application.Services.Class;

/// <summary>
/// Service upload/quản lý file lên Cloudflare R2 (tương thích S3)
/// </summary>
public class R2StorageService : IR2StorageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly string _publicBaseUrl;

    public R2StorageService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;

        // Đọc config R2 từ appsettings
        var r2 = configuration.GetSection("R2Storage");
        _bucketName = r2["BucketName"] ?? throw new InvalidOperationException("R2Storage:BucketName is required");
        _publicBaseUrl = (r2["PublicBaseUrl"] ?? $"{r2["AccountUrl"]?.TrimEnd('/')}/{_bucketName}").TrimEnd('/');

        var config = new AmazonS3Config
        {
            ServiceURL = r2["AccountUrl"],
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(r2["AccessKeyId"], r2["SecretAccessKey"], config);
    }

    #region Product Images

    /// <summary>Upload ảnh sản phẩm lên R2 và lưu vào DB</summary>
    public async Task<ProductImage> UploadProductImageToR2Async(
        Stream fileStream, string fileName, string contentType, Guid productId,
        CancellationToken cancellationToken = default)
    {
        var key = $"products/{productId}/{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        var imageUrl = await UploadFileToR2(fileStream, key, contentType, cancellationToken);

        var productImage = new ProductImage
        {
            ProductImageId = Guid.NewGuid(),
            ProductId = productId,
            ImageUrl = imageUrl,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.ProductImageRepository.AddAsync(productImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productImage;
    }

    /// <summary>Lấy danh sách ảnh của sản phẩm</summary>
    public async Task<IEnumerable<ProductImage>> GetImagesByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        => await _unitOfWork.ProductImageRepository.FindAsync(pi => pi.ProductId == productId);

    #endregion

    #region User Images

    /// <summary>Upload ảnh người dùng (avatar, cover...)</summary>
    public async Task<UserImage> UploadUserImageAsync(
        Stream fileStream, string fileName, string contentType, Guid userId,
        string imageType = "avatar", bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        var key = $"users/{userId}/{imageType}/{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        var imageUrl = await UploadFileToR2(fileStream, key, contentType, cancellationToken);

        // Nếu đặt làm ảnh chính → bỏ primary của các ảnh cũ
        if (isPrimary)
            await UnsetPrimaryImages(userId);

        var userImage = new UserImage
        {
            ImageId = Guid.NewGuid(),
            UserId = userId,
            ImageUrl = imageUrl,
            ImageType = imageType,
            IsPrimary = isPrimary,
            UploadedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<UserImage>().AddAsync(userImage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return userImage;
    }

    /// <summary>Lấy tất cả ảnh của user</summary>
    public async Task<IEnumerable<UserImage>> GetImagesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<UserImage>().FindAsync(ui => ui.UserId == userId);

    /// <summary>Lấy ảnh đại diện chính của user</summary>
    public async Task<UserImage?> GetPrimaryUserImageAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _unitOfWork.Repository<UserImage>().FirstOrDefaultAsync(ui => ui.UserId == userId && ui.IsPrimary);

    /// <summary>Đặt ảnh làm avatar chính</summary>
    public async Task<bool> SetPrimaryUserImageAsync(Guid userId, Guid imageId, CancellationToken cancellationToken = default)
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

    /// <summary>Xóa ảnh user (cả R2 và DB)</summary>
    public async Task<bool> DeleteUserImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _unitOfWork.Repository<UserImage>().FirstOrDefaultAsync(ui => ui.ImageId == imageId);
        if (image == null) return false;

        // Xóa file trên R2
        await TryDeleteFromR2(image.ImageUrl, cancellationToken);

        // Xóa trong DB
        _unitOfWork.Repository<UserImage>().Delete(image);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    #endregion

    #region Common Methods

    /// <summary>Lấy danh sách tất cả file trên bucket</summary>
    public async Task<List<S3Object>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = _bucketName }, cancellationToken);
        return response.S3Objects;
    }

    /// <summary>Upload file chung (không lưu DB)</summary>
    public async Task<object> UploadToR2Async(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"uploads/{Guid.NewGuid():N}_{SanitizeFileName(fileName)}";
        var url = await UploadFileToR2(fileStream, key, contentType, cancellationToken);
        return new { Key = key, Url = url };
    }

    /// <summary>Tạo URL có chữ ký để truy cập file private</summary>
    public string GeneratePreSignedUrl(string key, TimeSpan expiry)
    {
        AWSConfigsS3.UseSignatureVersion4 = true;
        return _s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry)
        });
    }

    /// <summary>Tạo PreSigned URL từ imageUrl</summary>
    public string? GetPreSignedUrlForImage(string imageUrl, TimeSpan? expiry = null)
    {
        var key = ExtractKeyFromUrl(imageUrl);
        return key != null ? GeneratePreSignedUrl(key, expiry ?? TimeSpan.FromHours(1)) : null;
    }

    #endregion

    #region Private Helpers

    /// <summary>Upload stream lên R2, trả về public URL</summary>
    private async Task<string> UploadFileToR2(Stream fileStream, string key, string contentType, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = memoryStream,
            ContentType = contentType,
            AutoCloseStream = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        }, cancellationToken);

        return $"{_publicBaseUrl}/{key}";
    }

    /// <summary>Bỏ flag primary của tất cả ảnh user</summary>
    private async Task UnsetPrimaryImages(Guid userId)
    {
        var images = await _unitOfWork.Repository<UserImage>().FindAsync(ui => ui.UserId == userId && ui.IsPrimary);
        foreach (var img in images)
        {
            img.IsPrimary = false;
            _unitOfWork.Repository<UserImage>().Update(img);
        }
    }

    /// <summary>Thử xóa file trên R2 (không throw lỗi)</summary>
    private async Task TryDeleteFromR2(string imageUrl, CancellationToken cancellationToken)
    {
        var key = ExtractKeyFromUrl(imageUrl);
        if (string.IsNullOrEmpty(key)) return;

        try
        {
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = _bucketName, Key = key }, cancellationToken);
        }
        catch { /* Bỏ qua lỗi xóa R2 */ }
    }

    /// <summary>Trích key từ URL (products/... hoặc users/...)</summary>
    private static string? ExtractKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var paths = new[] { "/products/", "/users/", "/uploads/" };
        foreach (var path in paths)
        {
            var index = url.IndexOf(path, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) return url[(index + 1)..];
        }
        return null;
    }

    /// <summary>Loại bỏ ký tự không hợp lệ trong tên file</summary>
    private static string SanitizeFileName(string fileName)
        => string.Join("_", fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

    #endregion
}

