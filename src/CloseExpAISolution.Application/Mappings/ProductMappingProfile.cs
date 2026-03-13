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
        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<StockLot, StockLotDetailDto>()
            .ForMember(dest => dest.UnitId, opt => opt.MapFrom(src => src.Product != null && src.Product.UnitOfMeasure != null ? src.Product.UnitOfMeasure.UnitId : Guid.Empty))
            .ForMember(dest => dest.UnitName, opt => opt.MapFrom(src => src.Product != null && src.Product.UnitOfMeasure != null ? src.Product.UnitOfMeasure.Name : ""))
            .ForMember(dest => dest.UnitType, opt => opt.MapFrom(src => src.Product != null && src.Product.UnitOfMeasure != null ? src.Product.UnitOfMeasure.Type : ""))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : ""))
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Product != null && src.Product.ProductDetail != null ? src.Product.ProductDetail.Brand : ""))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Product != null && src.Product.CategoryRef != null ? src.Product.CategoryRef.Name : ""))
            .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.Product != null ? src.Product.Barcode : ""))
            .ForMember(dest => dest.IsFreshFood, opt => opt.MapFrom(src => src.Product != null && src.Product.CategoryRef != null && src.Product.CategoryRef.IsFreshFood))
            .ForMember(dest => dest.WeightType, opt => opt.MapFrom(_ => ProductWeightType.Fixed))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(_ => "Định lượng cố định"))
            .ForMember(dest => dest.DefaultPricePerKg, opt => opt.MapFrom(_ => (decimal?)null))
            .ForMember(dest => dest.OriginalUnitPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.SuggestedUnitPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.FinalUnitPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.SupermarketId, opt => opt.MapFrom(src => src.Product != null ? src.Product.SupermarketId : Guid.Empty))
            .ForMember(dest => dest.SupermarketName, opt => opt.MapFrom(src => src.Product != null && src.Product.Supermarket != null ? src.Product.Supermarket.Name : ""))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(_ => (string?)null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(_ => new List<ProductImageDto>()))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.Product != null && src.Product.ProductDetail != null ? src.Product.ProductDetail.Ingredients : null))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => src.Product != null && src.Product.ProductDetail != null ? ParseNutritionFacts(src.Product.ProductDetail.NutritionFacts) : null))
            // Expiry status fields - set after mapping manually
            .ForMember(dest => dest.DaysRemaining, opt => opt.Ignore())
            .ForMember(dest => dest.HoursRemaining, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatusText, opt => opt.Ignore());

        CreateMap<Product, ProductDetailDto>()
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.Description ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.Origin ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Weight, opt => opt.MapFrom(src => src.StockLots != null && src.StockLots.Any()
                ? src.StockLots.OrderByDescending(pl => pl.ExpiryDate).First().Weight.ToString() + " " + (src.UnitOfMeasure != null ? src.UnitOfMeasure.Name : "")
                : "Đang cập nhật"))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.Ingredients ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.UsageInstructions, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.UsageInstructions ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.StorageInstructions, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.StorageInstructions ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.ManufactureDate, opt => opt.MapFrom(src => src.StockLots != null && src.StockLots.Any()
                ? src.StockLots.OrderByDescending(pl => pl.ExpiryDate).First().ManufactureDate.ToString("dd/MM/yyyy")
                : "Xem trên bao bì"))
            .ForMember(dest => dest.ExpiryDate, opt => opt.MapFrom(src => src.StockLots != null && src.StockLots.Any()
                ? src.StockLots.OrderByDescending(pl => pl.ExpiryDate).First().ExpiryDate.ToString("dd/MM/yyyy")
                : "Xem trên bao bì"))
            .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.Manufacturer ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.SafetyWarning, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.SafetyWarning ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.Distributor, opt => opt.MapFrom(src => src.ProductDetail != null ? (src.ProductDetail.Distributor ?? "Chưa có mô tả chi tiết") : "Chưa có mô tả chi tiết"))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => src.ProductDetail != null ? ParseNutritionFacts(src.ProductDetail.NutritionFacts) : null))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryRef != null ? src.CategoryRef.Name ?? "" : ""))
            .ForMember(dest => dest.Barcode, opt => opt.MapFrom(src => src.Barcode ?? string.Empty))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(_ => "Định lượng cố định"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)))
            .ForMember(dest => dest.SupermarketName, opt => opt.MapFrom(src => src.Supermarket != null ? src.Supermarket.Name : string.Empty))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(_ => (string?)null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(_ => new List<ProductImageDto>()))
            // Fields that need to be calculated/set manually after mapping
            .ForMember(dest => dest.UnitName, opt => opt.Ignore())
            .ForMember(dest => dest.Quantity, opt => opt.Ignore())
            .ForMember(dest => dest.DiscountPercent, opt => opt.Ignore())
            .ForMember(dest => dest.DaysToExpiry, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatus, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiryStatusText, opt => opt.Ignore());

        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Brand ?? "" : ""))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseProductState(src.Status)))
            .ForMember(dest => dest.WeightTypeName, opt => opt.MapFrom(_ => "Định lượng cố định"))
            .ForMember(dest => dest.MainImageUrl, opt => opt.MapFrom(_ => (string?)null))
            .ForMember(dest => dest.TotalImages, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.ProductImages, opt => opt.MapFrom(_ => new List<ProductImageDto>()))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Ingredients : null))
            .ForMember(dest => dest.NutritionFacts, opt => opt.MapFrom(src => src.ProductDetail != null ? ParseNutritionFacts(src.ProductDetail.NutritionFacts) : null))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryRef != null ? src.CategoryRef.Name ?? "" : ""))
            .ForMember(dest => dest.IsFreshFood, opt => opt.MapFrom(src => src.CategoryRef != null && src.CategoryRef.IsFreshFood))
            .ForMember(dest => dest.WeightType, opt => opt.MapFrom(_ => 1))
            .ForMember(dest => dest.DefaultPricePerKg, opt => opt.MapFrom(_ => (decimal?)null))
            .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.SuggestedPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.FinalPrice, opt => opt.MapFrom(_ => 0m))
            .ForMember(dest => dest.OcrConfidence, opt => opt.MapFrom(_ => 0f))
            .ForMember(dest => dest.PricingConfidence, opt => opt.MapFrom(_ => 0f))
            .ForMember(dest => dest.VerifiedBy, opt => opt.MapFrom(_ => (string?)null))
            .ForMember(dest => dest.VerifiedAt, opt => opt.MapFrom(_ => (DateTime?)null))
            .ForMember(dest => dest.PricedBy, opt => opt.MapFrom(_ => (string?)null))
            .ForMember(dest => dest.PricedAt, opt => opt.MapFrom(_ => (DateTime?)null));

        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Brand ?? string.Empty : string.Empty))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.CategoryRef != null ? src.CategoryRef.Name ?? string.Empty : string.Empty))
            .ForMember(dest => dest.Ingredients, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Ingredients ?? string.Empty : string.Empty))
            .ForMember(dest => dest.Nutrition, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.NutritionFacts ?? string.Empty : string.Empty))
            .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.UsageInstructions ?? string.Empty : string.Empty))
            .ForMember(dest => dest.Manufacturer, opt => opt.MapFrom(src => src.ProductDetail != null ? src.ProductDetail.Manufacturer ?? string.Empty : string.Empty));

        CreateMap<CreateProductRequestDto, Product>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => ProductState.Hidden.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UnitId, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryRef, opt => opt.Ignore())
            .ForMember(dest => dest.UnitOfMeasure, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.Ignore())
            .ForMember(dest => dest.StockLots, opt => opt.Ignore());

        CreateMap<UpdateProductRequestDto, Product>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ProductId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryRef, opt => opt.Ignore())
            .ForMember(dest => dest.UnitOfMeasure, opt => opt.Ignore())
            .ForMember(dest => dest.ProductDetail, opt => opt.Ignore())
            .ForMember(dest => dest.StockLots, opt => opt.Ignore());
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

