using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

/// <summary>
/// AutoMapper profile cho Product mappings
/// </summary>
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // ProductImage -> ProductImageDto
        CreateMap<ProductImage, ProductImageDto>();

        // Product -> ProductResponseDto
        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(src => GetWeightTypeName(src.WeightType)))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src =>
                src.ProductImages != null && src.ProductImages.Any()
                    ? src.ProductImages.OrderBy(i => i.UploadedAt).First().ImageUrl
                    : null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(src =>
                src.ProductImages != null ? src.ProductImages.Count : 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                src.ProductImages != null
                    ? src.ProductImages.OrderBy(i => i.UploadedAt).ToList()
                    : new List<ProductImage>()));

        // Product -> ProductDto
        CreateMap<Product, ProductDto>();

        // CreateProductRequestDto -> Product
        CreateMap<CreateProductRequestDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ProductState.Hidden.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Set manually from claims
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
            .ForMember(dest => dest.ProductLots, opt => opt.Ignore())
            .ForMember(dest => dest.AIVerificationLogs, opt => opt.Ignore());

        // UpdateProductRequestDto -> Product (for updates)
        CreateMap<UpdateProductRequestDto, Product>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
            .ForMember(dest => dest.ProductLots, opt => opt.Ignore())
            .ForMember(dest => dest.AIVerificationLogs, opt => opt.Ignore());
    }

    private static ProductState ParseProductState(string status)
    {
        return Enum.TryParse<ProductState>(status, out var result) ? result : ProductState.Hidden;
    }

    private static string GetWeightTypeName(int weightType)
    {
        return weightType switch
        {
            1 => "Định lượng cố định",
            2 => "Bán theo cân",
            _ => "Định lượng cố định"
        };
    }
}
