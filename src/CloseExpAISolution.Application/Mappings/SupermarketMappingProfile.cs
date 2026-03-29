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
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        CreateMap<Supermarket, SupermarketDto>();

        CreateMap<CreateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.SupermarketId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => SupermarketState.Active))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.ContactEmail, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicantUserId, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.AdminReviewNote, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicationReference, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicantUser, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());

        CreateMap<NewSupermarketRequest, Supermarket>()
            .ForMember(dest => dest.SupermarketId, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.Status, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Will be set manually
            .ForMember(dest => dest.ApplicantUserId, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.AdminReviewNote, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicationReference, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicantUser, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());

        CreateMap<UpdateSupermarketRequestDto, Supermarket>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.SupermarketId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ContactEmail, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicantUserId, opt => opt.Ignore())
            .ForMember(dest => dest.SubmittedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.AdminReviewNote, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicationReference, opt => opt.Ignore())
            .ForMember(dest => dest.ApplicantUser, opt => opt.Ignore())
            .ForMember(dest => dest.ReviewedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Products, opt => opt.Ignore())
            .ForMember(dest => dest.SupermarketStaffs, opt => opt.Ignore());
    }
}




