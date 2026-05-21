using AutoMapper;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class ProductImagePreSignedUrlResolver :
    IValueResolver<ProductImage, ProductImageDto, string?>,
    IValueResolver<ProductImage, CustomerProductImageResponseDto, string?>
{
    private static readonly TimeSpan PresignExpiry = TimeSpan.FromHours(1);

    private readonly IR2StorageService _r2Storage;

    public ProductImagePreSignedUrlResolver(IR2StorageService r2Storage)
    {
        _r2Storage = r2Storage;
    }

    public string? Resolve(
        ProductImage source,
        ProductImageDto destination,
        string? destMember,
        ResolutionContext context) =>
        _r2Storage.GetPreSignedUrlForImage(source.ImageUrl, PresignExpiry);

    public string? Resolve(
        ProductImage source,
        CustomerProductImageResponseDto destination,
        string? destMember,
        ResolutionContext context) =>
        _r2Storage.GetPreSignedUrlForImage(source.ImageUrl, PresignExpiry);
}
