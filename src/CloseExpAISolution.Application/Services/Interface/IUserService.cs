using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request);
    Task<ApiResponse<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto request);
    Task<ApiResponse<UserResponseDto>> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequestDto request);
    Task<ApiResponse<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}
