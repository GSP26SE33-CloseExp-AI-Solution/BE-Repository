using AutoMapper;
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
    private readonly IMapper _mapper;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    #region Public Methods

    public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync();
        var roleDictionary = await GetRoleDictionary();

        var userResponses = new List<UserResponseDto>();
        foreach (var user in users)
        {
            var dto = await MapUserWithRoleAndStaffInfoAsync(user, roleDictionary);
            userResponses.Add(dto);
        }

        return ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(userResponses);
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse);
    }

    public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        // Validate email uniqueness
        if (await EmailExists(request.Email))
            return Error("Email đã tồn tại");

        // Validate role
        var role = await GetRoleById(request.RoleId);
        if (role == null)
            return Error("Vai trò không hợp lệ");

        // Create user
        var user = _mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var userResponse = _mapper.Map<UserResponseDto>(user);
        userResponse.RoleName = role.RoleName;

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Tạo người dùng thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto request)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        // Validate email change
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await EmailExists(request.Email))
                return Error("Email đã tồn tại");
            user.Email = request.Email;
        }

        // Validate role change
        if (request.RoleId.HasValue)
        {
            var role = await GetRoleById(request.RoleId.Value);
            if (role == null)
                return Error("Vai trò không hợp lệ");
            user.RoleId = request.RoleId.Value;
        }

        // Update basic fields
        UpdateUserFields(user, request.FullName, request.Phone);

        if (request.Status.HasValue)
            user.Status = request.Status.Value.ToString();

        await SaveUserChanges(user);

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Cập nhật người dùng thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await FindUserById(userId);
        if (user == null)
            return NotFound();

        UpdateUserFields(user, request.FullName, request.Phone);
        await SaveUserChanges(user);

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Cập nhật thông tin cá nhân thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequestDto request)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        var oldStatus = user.Status;
        user.Status = request.Status.ToString();

        // Reset failed login count when verifying or unlocking
        if (request.Status == UserState.Verified)
            user.FailedLoginCount = 0;

        await SaveUserChanges(user);

        var userResponse = await MapUserWithRoleAsync(user);
        var statusMessage = GetStatusChangeMessage(oldStatus, request.Status.ToString());

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, statusMessage);
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        var user = await FindUserById(id);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");

        // Soft delete
        user.Status = UserState.Deleted.ToString();
        await SaveUserChanges(user);

        return ApiResponse<bool>.SuccessResponse(true, "Xóa người dùng thành công");
    }

    #endregion

    #region Private Helpers

    /// <summary>Finds user by ID, returns null if not found</summary>
    private async Task<User?> FindUserById(Guid id)
        => await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == id);

    /// <summary>Checks if email already exists in database</summary>
    private async Task<bool> EmailExists(string email)
    {
        var existingUser = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

    /// <summary>Gets role entity by ID</summary>
    private async Task<Role?> GetRoleById(int roleId)
        => await _unitOfWork.Repository<Role>().GetByIdAsync(roleId);

    /// <summary>Gets all roles as dictionary for bulk mapping</summary>
    private async Task<Dictionary<int, string>> GetRoleDictionary()
    {
        var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
        return roles.ToDictionary(r => r.RoleId, r => r.RoleName);
    }

    /// <summary>Maps user to DTO with role name lookup</summary>
    private async Task<UserResponseDto> MapUserWithRoleAsync(User user)
    {
        var role = await GetRoleById(user.RoleId);
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = role?.RoleName ?? "Unknown";

        // Nếu là MarketStaff (RoleId = 3) thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.MarketStaff)
        {
            dto.MarketStaffInfo = await GetMarketStaffInfoAsync(user.UserId);
        }

        return dto;
    }

    /// <summary>Maps user to DTO using pre-loaded role dictionary</summary>
    private UserResponseDto MapUserWithRole(User user, Dictionary<int, string> roleDictionary)
    {
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = roleDictionary.GetValueOrDefault(user.RoleId, "Unknown");
        return dto;
    }

    /// <summary>Maps user to DTO using pre-loaded role dictionary + load MarketStaff info if needed</summary>
    private async Task<UserResponseDto> MapUserWithRoleAndStaffInfoAsync(User user, Dictionary<int, string> roleDictionary)
    {
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = roleDictionary.GetValueOrDefault(user.RoleId, "Unknown");

        // Nếu là MarketStaff thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.MarketStaff)
        {
            dto.MarketStaffInfo = await GetMarketStaffInfoAsync(user.UserId);
        }

        return dto;
    }

    /// <summary>Lấy thông tin MarketStaff và Supermarket theo UserId</summary>
    private async Task<MarketStaffInfoDto?> GetMarketStaffInfoAsync(Guid userId)
    {
        var marketStaff = await _unitOfWork.Repository<MarketStaff>()
            .FirstOrDefaultAsync(ms => ms.UserId == userId);

        if (marketStaff == null)
            return null;

        var supermarket = await _unitOfWork.Repository<Supermarket>()
            .FirstOrDefaultAsync(s => s.SupermarketId == marketStaff.SupermarketId);

        return new MarketStaffInfoDto
        {
            MarketStaffId = marketStaff.MarketStaffId,
            Position = marketStaff.Position,
            JoinedAt = marketStaff.CreatedAt,
            Supermarket = supermarket == null ? null : new SupermarketBasicInfoDto
            {
                SupermarketId = supermarket.SupermarketId,
                Name = supermarket.Name,
                Address = supermarket.Address,
                ContactPhone = supermarket.ContactPhone
            }
        };
    }

    /// <summary>Updates user's basic info (name, phone) if provided</summary>
    private static void UpdateUserFields(User user, string? fullName, string? phone)
    {
        if (!string.IsNullOrEmpty(fullName))
            user.FullName = fullName;

        if (!string.IsNullOrEmpty(phone))
            user.Phone = phone;
    }

    /// <summary>Updates timestamp and saves user changes to database</summary>
    private async Task SaveUserChanges(User user)
    {
        user.UpdateAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Returns Vietnamese message for status change action</summary>
    private static string GetStatusChangeMessage(string oldStatus, string newStatus) => newStatus switch
    {
        nameof(UserState.Verified) => "Xác minh tài khoản thành công",
        nameof(UserState.Locked) => "Khóa tạm thời tài khoản thành công (30 phút)",
        nameof(UserState.Banned) => "Khóa vĩnh viễn tài khoản thành công",
        nameof(UserState.Unverified) => "Hủy xác minh tài khoản thành công",
        nameof(UserState.Hidden) => "Ẩn tài khoản thành công",
        nameof(UserState.Deleted) => "Xóa tài khoản thành công",
        _ => $"Cập nhật trạng thái từ {oldStatus} sang {newStatus} thành công"
    };

    /// <summary>Shortcut for user not found error</summary>
    private static ApiResponse<UserResponseDto> NotFound()
        => ApiResponse<UserResponseDto>.ErrorResponse("Không tìm thấy người dùng");

    /// <summary>Shortcut to create error response</summary>
    private static ApiResponse<UserResponseDto> Error(string message)
        => ApiResponse<UserResponseDto>.ErrorResponse(message);

    #endregion
}
