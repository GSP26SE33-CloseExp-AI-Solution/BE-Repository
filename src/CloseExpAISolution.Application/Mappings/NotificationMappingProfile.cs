using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Mappings;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Notification, NotificationResponseDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.Ignore());
    }
}
