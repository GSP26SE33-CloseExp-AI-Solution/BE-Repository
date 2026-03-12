using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Mappings;

/// <summary>
/// AutoMapper profile cho User mappings
/// </summary>
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // User -> UserResponseDto
        CreateMap<User, UserResponseDto>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : "Unknown"))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParseUserState(src.Status)));

        // User -> UserDto
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.RoleName : "Unknown"));

        // CreateUserRequestDto -> User
        CreateMap<CreateUserRequestDto, User>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Handle separately with BCrypt
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => UserState.Active.ToString())) // Admin creates active users
            .ForMember(dest => dest.FailedLoginCount, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.UserImages, opt => opt.Ignore())
            .ForMember(dest => dest.Feedbacks, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.DeliveryLogs, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.OtpCode, opt => opt.Ignore())
            .ForMember(dest => dest.OtpExpiresAt, opt => opt.Ignore())
            .ForMember(dest => dest.OtpFailedCount, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.GoogleId, opt => opt.Ignore());

        // UpdateUserRequestDto -> User (for partial updates)
        CreateMap<UpdateUserRequestDto, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }

    private static UserState ParseUserState(string status)
    {
        return Enum.TryParse<UserState>(status, out var result) ? result : UserState.Unverified;
    }
}

