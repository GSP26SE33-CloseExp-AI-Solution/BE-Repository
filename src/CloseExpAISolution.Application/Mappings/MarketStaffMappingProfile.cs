using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

/// <summary>
/// AutoMapper profile cho MarketStaff mappings
/// </summary>
public class MarketStaffMappingProfile : Profile
{
    public MarketStaffMappingProfile()
    {
        // MarketStaff -> MarketStaffResponseDto
        CreateMap<MarketStaff, MarketStaffResponseDto>();

        // MarketStaff -> MarketStaffDto
        CreateMap<MarketStaff, MarketStaffDto>();

        // CreateMarketStaffRequestDto -> MarketStaff
        CreateMap<CreateMarketStaffRequestDto, MarketStaff>()
            .ForMember(dest => dest.MarketStaffId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore());

        // UpdateMarketStaffRequestDto -> MarketStaff
        CreateMap<UpdateMarketStaffRequestDto, MarketStaff>()
            .ForMember(dest => dest.MarketStaffId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore());
    }
}
