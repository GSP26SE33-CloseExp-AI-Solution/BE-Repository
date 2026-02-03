using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CloseExpAISolution.Application.Services.Class;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    private const int MaxFailedLoginAttempts = 5;
    private const int LockoutDurationMinutes = 30;
    private const int RefreshTokenExpiryDays = 7;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    #region Public Methods

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Error("Email hoặc mật khẩu không hợp lệ");

        // Validate account status
        var statusError = await ValidateAndHandleAccountStatus(user, userRepository);
        if (statusError != null)
            return statusError;

        // Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return await HandleFailedLogin(user, userRepository);

        // Success: Reset failed attempts and generate tokens
        await ResetFailedLoginCount(user, userRepository);

        var roleName = await GetRoleName(user.RoleId);
        var authResponse = await GenerateTokensAsync(user, roleName, ipAddress, deviceInfo);

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Đăng nhập thành công");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();

        // Validate email uniqueness
        if (await EmailExists(request.Email))
            return Error("Email đã được đăng ký");

        // Validate role
        var roleId = (int)request.RegistrationType;
        var roleValidation = await ValidatePublicRegistrationRole(roleId);
        if (roleValidation != null)
            return roleValidation;

        // Create user
        var user = CreateNewUser(request, roleId);
        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AuthResponse>.SuccessWithMessage(
            "Đăng ký thành công. Vui lòng chờ Admin xác minh tài khoản của bạn trước khi đăng nhập");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var storedToken = await refreshTokenRepo.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken == null)
            return Error("Refresh token không hợp lệ");

        if (!storedToken.IsActive)
        {
            // If someone tries to use revoked/expired token, revoke all tokens for security
            if (storedToken.IsRevoked)
            {
                await RevokeAllUserTokensAsync(storedToken.UserId);
                return Error("Phát hiện token đã bị thu hồi được sử dụng lại. Tất cả phiên đăng nhập đã bị vô hiệu hóa");
            }
            return Error("Refresh token đã hết hạn");
        }

        // Get user
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.UserId == storedToken.UserId);
        if (user == null)
            return Error("Người dùng không tồn tại");

        // Check user status
        if (user.Status != UserState.Verified.ToString())
            return Error("Tài khoản không còn hoạt động");

        // Rotate token: revoke old, create new
        var newRefreshToken = GenerateRefreshTokenString();
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;
        refreshTokenRepo.Update(storedToken);

        // Create new refresh token
        var newToken = CreateRefreshTokenEntity(user.UserId, newRefreshToken, ipAddress, storedToken.DeviceInfo);
        await refreshTokenRepo.AddAsync(newToken);
        await _unitOfWork.SaveChangesAsync();

        // Generate new access token
        var roleName = await GetRoleName(user.RoleId);
        var authResponse = GenerateAuthResponse(user, roleName, newRefreshToken);

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Làm mới token thành công");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(string refreshToken)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var storedToken = await refreshTokenRepo.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken == null)
            return ApiResponse<bool>.ErrorResponse("Refresh token không hợp lệ");

        if (storedToken.IsRevoked)
            return ApiResponse<bool>.SuccessResponse(true, "Đã đăng xuất");

        // Revoke token
        storedToken.RevokedAt = DateTime.UtcNow;
        refreshTokenRepo.Update(storedToken);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công");
    }

    public async Task<ApiResponse<bool>> RevokeAllUserTokensAsync(Guid userId)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var activeTokens = await refreshTokenRepo.FindAsync(t => t.UserId == userId && t.RevokedAt == null);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            refreshTokenRepo.Update(token);
        }

        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<bool>.SuccessResponse(true, "Đã thu hồi tất cả phiên đăng nhập");
    }

    #endregion

    #region Login Helpers

    /// <summary>Checks account status and auto-unlocks if lockout expired</summary>
    private async Task<ApiResponse<AuthResponse>?> ValidateAndHandleAccountStatus(User user, dynamic userRepository)
    {
        var status = user.Status;

        if (status == UserState.Unverified.ToString())
            return Error("Tài khoản chưa được xác minh. Vui lòng chờ Admin phê duyệt");

        if (status == UserState.Locked.ToString())
        {
            var unlockResult = TryAutoUnlock(user);
            if (!unlockResult.IsUnlocked)
                return Error($"Tài khoản đang bị khóa tạm thời. Vui lòng thử lại sau {unlockResult.RemainingMinutes} phút");

            // Save the unlock
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
        }

        if (status == UserState.Banned.ToString())
            return Error("Tài khoản đã bị khóa vĩnh viễn bởi Admin");

        if (status == UserState.Deleted.ToString())
            return Error("Tài khoản đã bị xóa");

        return null;
    }

    /// <summary>Attempts to unlock account if 30-min lockout has passed</summary>
    private (bool IsUnlocked, int RemainingMinutes) TryAutoUnlock(User user)
    {
        var lockoutEndTime = user.UpdateAt.AddMinutes(LockoutDurationMinutes);
        var remainingTime = lockoutEndTime - DateTime.UtcNow;

        if (remainingTime > TimeSpan.Zero)
            return (false, (int)Math.Ceiling(remainingTime.TotalMinutes));

        // Auto-unlock
        user.Status = UserState.Verified.ToString();
        user.FailedLoginCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        return (true, 0);
    }

    /// <summary>Increments failed count and locks account after 5 attempts</summary>
    private async Task<ApiResponse<AuthResponse>> HandleFailedLogin(User user, dynamic userRepository)
    {
        user.FailedLoginCount++;
        user.UpdateAt = DateTime.UtcNow;

        if (user.FailedLoginCount >= MaxFailedLoginAttempts)
        {
            user.Status = UserState.Locked.ToString();
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return Error($"Tài khoản đã bị khóa tạm thời do đăng nhập sai quá {MaxFailedLoginAttempts} lần. Vui lòng thử lại sau {LockoutDurationMinutes} phút");
        }

        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var attemptsLeft = MaxFailedLoginAttempts - user.FailedLoginCount;
        return Error($"Email hoặc mật khẩu không hợp lệ. Còn {attemptsLeft} lần thử");
    }

    /// <summary>Resets failed login counter after successful login</summary>
    private async Task ResetFailedLoginCount(User user, dynamic userRepository)
    {
        if (user.FailedLoginCount == 0) return;

        user.FailedLoginCount = 0;
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>Verifies password against BCrypt hash</summary>
    private static bool VerifyPassword(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    #endregion

    #region Registration Helpers

    /// <summary>Checks if email is already registered</summary>
    private async Task<bool> EmailExists(string email)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

    /// <summary>Validates role is allowed for public registration (Vendor/MarketStaff only)</summary>
    private async Task<ApiResponse<AuthResponse>?> ValidatePublicRegistrationRole(int roleId)
    {
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(roleId);

        if (role == null)
            return Error("Loại đăng ký không hợp lệ");

        // Only Vendor and MarketStaff can register publicly
        if (roleId != (int)RoleUser.Vendor && roleId != (int)RoleUser.MarketStaff)
            return Error("Loại đăng ký này không được phép. Chỉ Vendor và MarketStaff mới có thể đăng ký công khai.");

        return null;
    }

    /// <summary>Creates a new user entity with Unverified status</summary>
    private static User CreateNewUser(RegisterRequest request, int roleId) => new()
    {
        UserId = Guid.NewGuid(),
        FullName = request.FullName,
        Email = request.Email,
        Phone = request.Phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        RoleId = roleId,
        Status = UserState.Unverified.ToString(),
        FailedLoginCount = 0,
        CreatedAt = DateTime.UtcNow,
        UpdateAt = DateTime.UtcNow
    };

    #endregion

    #region Token Generation

    /// <summary>Generates JWT access token and refresh token, saves refresh token to DB</summary>
    private async Task<AuthResponse> GenerateTokensAsync(User user, string roleName, string? ipAddress, string? deviceInfo)
    {
        var refreshTokenString = GenerateRefreshTokenString();

        // Save refresh token to database
        var refreshToken = CreateRefreshTokenEntity(user.UserId, refreshTokenString, ipAddress, deviceInfo);
        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return GenerateAuthResponse(user, roleName, refreshTokenString);
    }

    /// <summary>Generates AuthResponse with access token</summary>
    private AuthResponse GenerateAuthResponse(User user, string roleName, string refreshToken)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var accessToken = GenerateAccessToken(user, roleName, jwtSettings, expiresAt);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserResponse(user, roleName)
        };
    }

    /// <summary>Creates a secure random refresh token string</summary>
    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>Creates RefreshToken entity for database</summary>
    private static RefreshToken CreateRefreshTokenEntity(Guid userId, string token, string? ipAddress, string? deviceInfo) => new()
    {
        RefreshTokenId = Guid.NewGuid(),
        UserId = userId,
        Token = token,
        ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
        CreatedAt = DateTime.UtcNow,
        IpAddress = ipAddress,
        DeviceInfo = deviceInfo
    };

    /// <summary>Creates JWT token with user claims</summary>
    private static string GenerateAccessToken(User user, string roleName, IConfigurationSection jwtSettings, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("RoleId", user.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Maps User entity to UserResponseDto</summary>
    private static UserResponseDto MapToUserResponse(User user, string roleName) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        RoleName = roleName,
        RoleId = user.RoleId,
        Status = Enum.TryParse<UserState>(user.Status, out var status) ? status : UserState.Unverified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdateAt
    };

    #endregion

    #region Common Helpers

    /// <summary>Gets role name by roleId from database</summary>
    private async Task<string> GetRoleName(int roleId)
    {
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(roleId);
        return role?.RoleName ?? "User";
    }

    /// <summary>Shortcut to create error response</summary>
    private static ApiResponse<AuthResponse> Error(string message)
        => ApiResponse<AuthResponse>.ErrorResponse(message);

    #endregion
}
