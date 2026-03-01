using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

/// <summary>
/// AutoMapper profile cho Supermarket mappings
/// </summary>
public class SupermarketMappingProfile : Profile
{
    public SupermarketMappingProfile()
    {
        // Supermarket -> SupermarketResponseDto
        CreateMap<Supermarket, SupermarketResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseUserState(src.Status)));

        // Supermarket -> SupermarketDto
        CreateMap<Supermarket, SupermarketDto>();

        // CreateSupermarketRequestDto -> Supermarket
        CreateMap<CreateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.SupermarketId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => UserState.Active.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.MarketStaff, opt => opt.Ignore());

        // UpdateSupermarketRequestDto -> Supermarket
        CreateMap<UpdateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.SupermarketId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.MarketStaff, opt => opt.Ignore());
    }

    private static UserState ParseUserState(string status)
    {
        return Enum.TryParse<UserState>(status, out var result) ? result : UserState.Active;
    }
}
