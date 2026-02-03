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
        // Product -> ProductResponseDto
        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)));

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
}
