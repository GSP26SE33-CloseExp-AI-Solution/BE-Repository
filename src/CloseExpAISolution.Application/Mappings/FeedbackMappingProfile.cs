using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        CreateMap<CustomerFeedback, FeedbackResponseDto>()
            .ForMember(dest => dest.UserName, opt => opt.Ignore());

        CreateMap<CreateFeedbackRequestDto, CustomerFeedback>()
            .ForMember(dest => dest.CustomerFeedbackId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore());
    }
}
