using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserResponse>>> GetAllUsersAsync();
    Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid id);
    Task<ApiResponse<UserResponse>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserResponse>> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(Guid id);
}
