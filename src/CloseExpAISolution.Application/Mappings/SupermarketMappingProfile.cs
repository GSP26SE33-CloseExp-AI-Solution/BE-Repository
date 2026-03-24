using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

public class SupermarketMappingProfile : Profile
{
    public SupermarketMappingProfile()
    {
        CreateMap<Supermarket, SupermarketResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseSupermarketState(src.Status)));

        CreateMap<Supermarket, SupermarketDto>();

        CreateMap<CreateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.SupermarketId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => SupermarketState.Active.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());

        CreateMap<NewSupermarketRequest, Supermarket>()
            .ForMember(dest => dest.SupermarketId, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());

        CreateMap<UpdateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.SupermarketId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());
    }

    private static SupermarketState ParseSupermarketState(string status)
    {
        return Enum.TryParse<SupermarketState>(status, out var result) ? result : SupermarketState.Active;
    }
}

