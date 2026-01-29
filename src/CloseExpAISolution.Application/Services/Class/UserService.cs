using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
    {
        var userRepository = _unitOfWork.Repository<User>();
        var roleRepository = _unitOfWork.Repository<Role>();

        var users = await userRepository.GetAllAsync();
        var roles = await roleRepository.GetAllAsync();
        var roleDictionary = roles.ToDictionary(r => r.RoleId, r => r.RoleName);

        var userResponses = users.Select(u => new UserResponseDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            RoleId = u.RoleId,
            RoleName = roleDictionary.GetValueOrDefault(u.RoleId, "Unknown"),
            Status = Enum.TryParse<UserState>(u.Status, out var status) ? status : UserState.Active,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdateAt
        });

        return ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(userResponses);
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return ApiResponse<UserResponseDto>.ErrorResponse("Không tìm thấy người dùng");
        }

        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(user.RoleId);

        var userResponse = new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = role?.RoleName ?? "Unknown",
            Status = Enum.TryParse<UserState>(user.Status, out var status) ? status : UserState.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdateAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse);
    }

    public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        var userRepository = _unitOfWork.Repository<User>();

        // Check if email already exists
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return ApiResponse<UserResponseDto>.ErrorResponse("Email đã tồn tại");
        }

        // Verify role exists
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            return ApiResponse<UserResponseDto>.ErrorResponse("Vai trò không hợp lệ");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId,
            Status = UserState.Active.ToString(),
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var userResponse = new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = role.RoleName,
            Status = UserState.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdateAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Tạo người dùng thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return ApiResponse<UserResponseDto>.ErrorResponse("Không tìm thấy người dùng");
        }

        // Check if email is being changed and if it already exists
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return ApiResponse<UserResponseDto>.ErrorResponse("Email đã tồn tại");
            }
            user.Email = request.Email;
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(request.FullName))
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.Phone))
            user.Phone = request.Phone;

        if (request.Status.HasValue)
            user.Status = request.Status.Value.ToString();

        if (request.RoleId.HasValue)
        {
            var roleRepository = _unitOfWork.Repository<Role>();
            var role = await roleRepository.GetByIdAsync(request.RoleId.Value);
            if (role == null)
            {
                return ApiResponse<UserResponseDto>.ErrorResponse("Vai trò không hợp lệ");
            }
            user.RoleId = request.RoleId.Value;
        }

        user.UpdateAt = DateTime.UtcNow;

        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Get role name
        var roleRepo = _unitOfWork.Repository<Role>();
        var userRole = await roleRepo.GetByIdAsync(user.RoleId);

        var userResponse = new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = userRole?.RoleName ?? "Unknown",
            Status = Enum.TryParse<UserState>(user.Status, out var status) ? status : UserState.Active,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdateAt
        };

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Cập nhật người dùng thành công");
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");
        }

        // Soft delete - change status to Deleted
        user.Status = UserState.Deleted.ToString();
        user.UpdateAt = DateTime.UtcNow;

        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Xóa người dùng thành công");
    }
}
