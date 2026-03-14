using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
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
        if (request.Status == UserState.Active)
            user.FailedLoginCount = 0;

        await SaveUserChanges(user);
        // Send email notification on approve/reject
        await SendStatusChangeEmailAsync(user, oldStatus, request.Status);
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

    public async Task<ApiResponse<bool>> DeleteOwnAccountAsync(Guid userId)
    {
        var user = await FindUserById(userId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");

        // Nhân viên siêu thị không thể tự xóa tài khoản
        if (user.RoleId == (int)RoleUser.SupplierStaff)
            return ApiResponse<bool>.ErrorResponse("Nhân viên siêu thị không thể tự xóa tài khoản. Vui lòng liên hệ Admin");

        if (user.Status == UserState.Deleted.ToString())
            return ApiResponse<bool>.ErrorResponse("Tài khoản đã bị xóa trước đó");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Soft delete user
            user.Status = UserState.Deleted.ToString();
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<User>().Update(user);

            // Revoke tất cả refresh token
            var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
            var activeTokens = await refreshTokenRepo.FindAsync(t => t.UserId == userId && t.RevokedAt == null);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                refreshTokenRepo.Update(token);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to delete own account for user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse("Xóa tài khoản thất bại. Vui lòng thử lại sau");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa tài khoản thành công");
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

        // Nếu là SupplierStaff (RoleId = 4) - nhân viên siêu thị thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.SupplierStaff)
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

        // Nếu là SupplierStaff (nhân viên siêu thị) thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.SupplierStaff)
        {
            dto.MarketStaffInfo = await GetMarketStaffInfoAsync(user.UserId);
        }

        return dto;
    }

    /// <summary>Lấy thông tin MarketStaff và Supermarket theo UserId</summary>
    private async Task<MarketStaffInfoDto?> GetMarketStaffInfoAsync(Guid userId)
    {
        var marketStaff = await _unitOfWork.Repository<SupermarketStaff>()
            .FirstOrDefaultAsync(ms => ms.UserId == userId);

        if (marketStaff == null)
            return null;

        var supermarket = await _unitOfWork.Repository<Supermarket>()
            .FirstOrDefaultAsync(s => s.SupermarketId == marketStaff.SupermarketId);

        return new MarketStaffInfoDto
        {
            MarketStaffId = marketStaff.SupermarketStaffId,
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
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Returns Vietnamese message for status change action</summary>
    private static string GetStatusChangeMessage(string oldStatus, string newStatus) => newStatus switch
    {
        nameof(UserState.Active) => "Phê duyệt tài khoản thành công",
        nameof(UserState.PendingApproval) => "Chuyển tài khoản sang chờ phê duyệt",
        nameof(UserState.Rejected) => "Từ chối phê duyệt tài khoản",
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

    /// <summary>Sends email notification when admin changes user status (approve/reject)</summary>
    private async Task SendStatusChangeEmailAsync(User user, string oldStatus, UserState newStatus)
    {
        try
        {
            if (oldStatus == UserState.PendingApproval.ToString() && newStatus == UserState.Active)
            {
                var subject = "CloseExp AI - Tài khoản đã được phê duyệt!";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='color: white; margin: 0;'>🎉 Tài Khoản Đã Được Phê Duyệt!</h1>
                        </div>
                        <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                            <h2>Xin chào {user.FullName}!</h2>
                            <p>Tài khoản của bạn đã được <strong>Admin phê duyệt thành công</strong>.</p>
                            <p>Bạn có thể đăng nhập vào hệ thống CloseExp AI ngay bây giờ!</p>
                            <p style='color: #999; font-size: 12px;'>Cảm ơn bạn đã sử dụng CloseExp AI!</p>
                        </div>
                    </body>
                    </html>";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
            else if (oldStatus == UserState.PendingApproval.ToString() && newStatus == UserState.Rejected)
            {
                var subject = "CloseExp AI - Tài khoản không được phê duyệt";
                var body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                            <h1 style='color: white; margin: 0;'>Thông Báo Về Tài Khoản</h1>
                        </div>
                        <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                            <h2>Xin chào {user.FullName},</h2>
                            <p>Rất tiếc, tài khoản của bạn <strong>không được phê duyệt</strong> bởi Admin.</p>
                            <p>Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ với đội ngũ hỗ trợ.</p>
                            <p style='color: #999; font-size: 12px;'>CloseExp AI Team</p>
                        </div>
                    </body>
                    </html>";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status change email to {Email}", user.Email);
        }
    }

    #endregion
}
