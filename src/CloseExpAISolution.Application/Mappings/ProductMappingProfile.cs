using System.Text.Json;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
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

        // ProductLot -> ProductLotDetailDto (với custom logic cho expiry status)
        CreateMap<ProductLot, ProductLotDetailDto>()
            .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.Name : ""))
            .ForMember(dest => dest.UnitType, opt => opt.MapFrom(src => src.Unit != null ? src.Unit.Type : ""))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : ""))
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Product != null ? src.Product.Brand : ""))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Product != null ? src.Product.Category : ""))
            .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Barcode : ""))
            .ForMember(dest => dest.IsFreshFood, opt => opt.MapFrom(src => src.Product != null && src.Product.IsFreshFood))
            .ForMember(dest => dest.WeightType, opt => opt.MapFrom(src => src.Product != null ? (ProductWeightType)src.Product.WeightType : ProductWeightType.Fixed))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(src => src.Product != null ? GetWeightTypeName(src.Product.WeightType) : ""))
            .ForMember(dest => dest.DefaultPricePerKg, opt => opt.MapFrom(src => src.Product != null ? src.Product.DefaultPricePerKg : null))
            .ForMember(dest => dest.SupermarketId, opt => opt.MapFrom(src => src.Product != null ? src.Product.SupermarketId : Guid.Empty))
            .ForMember(dest => dest.SupermarketName, opt => opt.MapFrom(src => src.Product != null && src.Product.Supermarket != null ? src.Product.Supermarket.Name : ""))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src =>
                src.Product != null && src.Product.ProductImages != null && src.Product.ProductImages.Any()
                    ? src.Product.ProductImages.OrderBy(i => i.UploadedAt).First().ImageUrl
                    : null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(src =>
                src.Product != null && src.Product.ProductImages != null ? src.Product.ProductImages.Count : 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                src.Product != null && src.Product.ProductImages != null
                    ? src.Product.ProductImages.OrderBy(i => i.UploadedAt).Select(img => new ProductImageDto
                    {
                        ProductImageId = img.ProductImageId,
                        ProductId = img.ProductId,
                        ImageUrl = img.ImageUrl,
                        UploadedAt = img.UploadedAt
                    }).ToList()
                    : new List<ProductImageDto>()))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Product != null ? src.Product.Ingredients : null))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => src.Product != null ? ParseNutritionFacts(src.Product.NutritionFactsJson) : null))
            // Expiry status fields - set after mapping manually
            .ForMember(dest => dest.DaysRemaining, opt => opt.Ignore())
            .ForMember(dest => dest.HoursRemaining, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatusText, opt => opt.Ignore());

        // Product -> ProductDetailDto
        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Origin ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.Weight ?? "Đang cập nhật"))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Ingredients ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.UsageInstructions, opt => opt.MapFrom(src => src.UsageInstructions ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.StorageInstructions, opt => opt.MapFrom(src => src.StorageInstructions ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.ManufactureDate, opt => opt.MapFrom(src => src.ManufactureDate.HasValue ? src.ManufactureDate.Value.ToString("dd/MM/yyyy") : "Xem trên bao bì"))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.ExpiryDate.HasValue ? src.ExpiryDate.Value.ToString("dd/MM/yyyy") : "Xem trên bao bì"))
            .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.Manufacturer ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.SafetyWarning, opt => opt.MapFrom(src => src.SafetyWarning ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Distributor, opt => opt.MapFrom(src => src.Distributor ?? "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => ParseNutritionFacts(src.NutritionFactsJson)))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category ?? string.Empty))
            .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.Barcode ?? string.Empty))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(src => GetWeightTypeName(src.WeightType)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)))
            .ForMember(dest => dest.SupermarketName, opt => opt.MapFrom(src => src.Supermarket != null ? src.Supermarket.Name : string.Empty))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(src =>
                src.ProductImages != null && src.ProductImages.Any()
                    ? src.ProductImages.OrderBy(i => i.UploadedAt).First().ImageUrl
                    : null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(src =>
                src.ProductImages != null ? src.ProductImages.Count : 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(src =>
                src.ProductImages != null
                    ? src.ProductImages.OrderBy(i => i.UploadedAt).Select(img => new ProductImageDto
                    {
                        ProductImageId = img.ProductImageId,
                        ProductId = img.ProductId,
                        ImageUrl = img.ImageUrl,
                        UploadedAt = img.UploadedAt
                    }).ToList()
                    : new List<ProductImageDto>()))
            // Fields that need to be calculated/set manually after mapping
            .ForMember(dest => dest.UnitName, opt => opt.Ignore())
            .ForMember(dest => dest.Quantity, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountPercent, opt => opt.Ignore())
            .ForMember(dest => dest.DaysToExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatusText, opt => opt.Ignore());

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
                    : new List<ProductImage>()))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Ingredients))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => ParseNutritionFacts(src.NutritionFactsJson)));

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
