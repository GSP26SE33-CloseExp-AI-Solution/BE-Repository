using System.Text.Json;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Pricing, PricingResponseDto>();

        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)))
            .ForMember(dest => dest.Pricing, opt => opt.MapFrom(src => src.Pricing));

        CreateMap<Product, ProductDto>();

        CreateMap<CreateProductRequestDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ProductState.Hidden.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
            .ForMember(dest => dest.ProductLots, opt => opt.Ignore())
            .ForMember(dest => dest.AIVerificationLogs, opt => opt.Ignore())
            .ForMember(dest => dest.Pricing, opt => opt.Ignore());

        CreateMap<UpdateProductRequestDto, Product>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.ProductImages, opt => opt.Ignore())
            .ForMember(dest => dest.ProductLots, opt => opt.Ignore())
            .ForMember(dest => dest.AIVerificationLogs, opt => opt.Ignore())
            .ForMember(dest => dest.Pricing, opt => opt.Ignore());
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

    /// <summary>
    /// Parse chuỗi JSON thành Dictionary chứa thông tin dinh dưỡng
    /// </summary>
    private static Dictionary<string, string>? ParseNutritionFacts(string? nutritionFactsJson)
    {
        if (string.IsNullOrEmpty(nutritionFactsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(nutritionFactsJson);
        }
        catch
        {
            return null;
        }
    }
}
