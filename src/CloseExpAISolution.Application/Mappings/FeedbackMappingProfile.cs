using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class FeedbackMappingProfile : Profile
{
    public FeedbackMappingProfile()
    {
        // Feedback -> FeedbackResponseDto
        CreateMap<Feedback, FeedbackResponseDto>()
            .ForMember(dest => dest.UserName, opt => opt.Ignore()); // Set manually

        // CreateFeedbackRequestDto -> Feedback
        CreateMap<CreateFeedbackRequestDto, Feedback>()
            .ForMember(dest => dest.FeedbackId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore());
    }
}
