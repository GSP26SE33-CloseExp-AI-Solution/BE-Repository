using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

public class MarketStaffMappingProfile : Profile
{
    public MarketStaffMappingProfile()
    {
        CreateMap<SupermarketStaff, MarketStaffResponseDto>();

        CreateMap<SupermarketStaff, MarketStaffDto>();

        CreateMap<CreateMarketStaffRequestDto, SupermarketStaff>()
            .ForMember(dest => dest.SupermarketStaffId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => SupermarketStaffState.Active))
            .ForMember(dest => dest.IsManager, opt => opt.MapFrom(_ => false))
            .ForMember(dest => dest.EmployeeCodeHash, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeCodeHint, opt => opt.Ignore())
            .ForMember(dest => dest.ParentSuperStaffId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentSuperStaff, opt => opt.Ignore())
            .ForMember(dest => dest.SubordinateStaff, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore());

        CreateMap<UpdateMarketStaffRequestDto, SupermarketStaff>()
            .ForMember(dest => dest.SupermarketStaffId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.IsManager, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeCodeHash, opt => opt.Ignore())
            .ForMember(dest => dest.EmployeeCodeHint, opt => opt.Ignore())
            .ForMember(dest => dest.ParentSuperStaffId, opt => opt.Ignore())
            .ForMember(dest => dest.ParentSuperStaff, opt => opt.Ignore())
            .ForMember(dest => dest.SubordinateStaff, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Supermarket, opt => opt.Ignore());
    }
}