using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CloseExpAISolution.Application.Services.Class;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;
    private readonly IMapboxService _mapboxService;

    private const int MaxFailedLoginAttempts = 5;
    private const int LockoutDurationMinutes = 30;
    private const int RefreshTokenExpiryDays = 7;
    private const int OtpExpiryMinutes = 5;
    private const int OtpResendCooldownSeconds = 60;
    private const int MaxOtpFailedAttempts = 5;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration, IEmailService emailService, ILogger<AuthService> logger, IMapper mapper, IMapboxService mapboxService)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
        _mapper = mapper;
        _mapboxService = mapboxService;
    }

    #region Public Methods

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null, string? deviceInfo = null)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return Error("Email hoặc mật khẩu không hợp lệ");

        var statusError = await ValidateAndHandleAccountStatus(user, userRepository);
        if (statusError != null)
            return statusError;

        if (!VerifyPassword(request.Password, user.PasswordHash))
            return await HandleFailedLogin(user, userRepository);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Reset failed login count
            if (user.FailedLoginCount > 0)
            {
                user.FailedLoginCount = 0;
                userRepository.Update(user);
            }

            // Generate and save refresh token
            var roleName = await GetRoleName(user.RoleId);
            var authResponse = await GenerateTokensAsync(user, roleName, ipAddress, deviceInfo);

            await _unitOfWork.CommitTransactionAsync();

            return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Đăng nhập thành công");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to complete login for user {Email}", request.Email);
            return Error("Đăng nhập thất bại. Vui lòng thử lại sau");
        }
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();

        if (await EmailExists(request.Email))
            return Error("Email đã được đăng ký");

        var roleId = (int)request.RegistrationType;
        var roleValidation = await ValidatePublicRegistrationRole(roleId);
        if (roleValidation != null)
            return roleValidation;

        // XỬ LÝ ĐĂNG KÝ SUPPLIERSTAFF
        Guid? finalSupermarketId = null;
        if (roleId == (int)RoleUser.SupplierStaff)
        {
            if (request.NewSupermarket == null)
                return Error("Vui lòng nhập thông tin siêu thị/cơ sở");

            var existingSupermarket = await _unitOfWork.Repository<Supermarket>()
                .FirstOrDefaultAsync(s =>
                    s.Name.ToLower() == request.NewSupermarket.Name.ToLower() &&
                    s.Address.ToLower() == request.NewSupermarket.Address.ToLower() &&
                    s.Latitude == request.NewSupermarket.Latitude &&
                    s.Longitude == request.NewSupermarket.Longitude);

            if (existingSupermarket != null)
                return Error($"Cơ sở '{existingSupermarket.Name} - {existingSupermarket.Address}' đã tồn tại trong hệ thống");

            // Tạo tọa độ nếu chỉ nhập địa chỉ
            if (request.NewSupermarket.Latitude == 0 && request.NewSupermarket.Longitude == 0
                && !string.IsNullOrWhiteSpace(request.NewSupermarket.Address))
            {
                try
                {
                    var geocodeResult = await _mapboxService.ForwardGeocodeAsync(request.NewSupermarket.Address);
                    if (geocodeResult != null)
                    {
                        request.NewSupermarket.Latitude = (decimal)geocodeResult.Latitude;
                        request.NewSupermarket.Longitude = (decimal)geocodeResult.Longitude;
                        _logger.LogInformation("Auto-geocoded address '{Address}' → ({Lat}, {Lng})",
                            request.NewSupermarket.Address, geocodeResult.Latitude, geocodeResult.Longitude);
                    }
                    else
                    {
                        _logger.LogWarning("Mapbox geocoding returned no result for '{Address}'", request.NewSupermarket.Address);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Mapbox geocoding failed for '{Address}', continuing with Lat=0/Lng=0", request.NewSupermarket.Address);
                }
            }

            finalSupermarketId = Guid.NewGuid();
        }

        var user = CreateNewUser(request, roleId);

        var otp = GenerateOtp();
        user.OtpCode = HashOtp(otp);
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await userRepository.AddAsync(user);

            if (roleId == (int)RoleUser.SupplierStaff && finalSupermarketId.HasValue)
            {
                var newSupermarket = _mapper.Map<Supermarket>(request.NewSupermarket);
                newSupermarket.SupermarketId = finalSupermarketId.Value;
                newSupermarket.Status = UserState.Active.ToString();
                newSupermarket.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Supermarket>().AddAsync(newSupermarket);

                var marketStaff = new MarketStaff
                {
                    MarketStaffId = Guid.NewGuid(),
                    UserId = user.UserId,
                    SupermarketId = finalSupermarketId.Value,
                    Position = request.Position ?? "Nhân viên",
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<MarketStaff>().AddAsync(marketStaff);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to register user {Email}", request.Email);
            return Error("Đăng ký thất bại. Vui lòng thử lại sau");
        }

        await SendOtpEmailAsync(user.Email, otp, user.FullName);

        return ApiResponse<AuthResponse>.SuccessWithMessage(
            "Đăng ký thành công. Vui lòng kiểm tra email để nhập mã OTP xác nhận");
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress = null)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var storedToken = await refreshTokenRepo.FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (storedToken == null)
            return Error("Refresh token không hợp lệ");

        if (!storedToken.IsActive)
        {
            // If someone tries to use revoked/expired token, revoke all tokens
            if (storedToken.IsRevoked)
            {
                await RevokeAllUserTokensAsync(storedToken.UserId);
                return Error("Phát hiện token đã bị thu hồi được sử dụng lại. Tất cả phiên đăng nhập đã bị vô hiệu hóa");
            }
            return Error("Refresh token đã hết hạn");
        }

        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.UserId == storedToken.UserId);
        if (user == null)
            return Error("Người dùng không tồn tại");

        if (user.Status != UserState.Active.ToString())
            return Error("Tài khoản không còn hoạt động");

        // Rotate token: revoke old, create new
        var newRefreshToken = GenerateRefreshTokenString();
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = newRefreshToken;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            refreshTokenRepo.Update(storedToken);

            var newToken = CreateRefreshTokenEntity(user.UserId, newRefreshToken, ipAddress, storedToken.DeviceInfo);
            await refreshTokenRepo.AddAsync(newToken);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to refresh token for user {UserId}", user.UserId);
            return Error("Làm mới token thất bại. Vui lòng thử lại");
        }

        // Generate new access token
        var roleName = await GetRoleName(user.RoleId);
        var authResponse = await GenerateAuthResponseAsync(user, roleName, newRefreshToken);

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

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            refreshTokenRepo.Update(storedToken);
            await _unitOfWork.CommitTransactionAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Đăng xuất thành công");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to logout token");
            return ApiResponse<bool>.ErrorResponse("Đăng xuất thất bại. Vui lòng thử lại");
        }
    }

    public async Task<ApiResponse<bool>> RevokeAllUserTokensAsync(Guid userId)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var activeTokens = await refreshTokenRepo.FindAsync(t => t.UserId == userId && t.RevokedAt == null);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                refreshTokenRepo.Update(token);
            }

            await _unitOfWork.CommitTransactionAsync();
            return ApiResponse<bool>.SuccessResponse(true, "Đã thu hồi tất cả phiên đăng nhập");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse("Thu hồi phiên đăng nhập thất bại. Vui lòng thử lại");
        }
    }

    public async Task<ApiResponse<bool>> VerifyOtpAsync(VerifyOtpRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Email không tồn tại");

        if (user.Status != UserState.Unverified.ToString())
            return ApiResponse<bool>.ErrorResponse("Tài khoản không cần xác minh email");

        if (user.OtpFailedCount >= MaxOtpFailedAttempts)
            return ApiResponse<bool>.ErrorResponse("Nhập sai OTP quá nhiều lần. Vui lòng yêu cầu gửi lại mã OTP mới");

        if (string.IsNullOrEmpty(user.OtpCode) || user.OtpExpiresAt == null || user.OtpExpiresAt < DateTime.UtcNow)
            return ApiResponse<bool>.ErrorResponse("Mã OTP đã hết hạn. Vui lòng yêu cầu gửi lại mã mới");

        if (user.OtpCode != HashOtp(request.OtpCode))
        {
            user.OtpFailedCount++;
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            var attemptsLeft = MaxOtpFailedAttempts - user.OtpFailedCount;
            return ApiResponse<bool>.ErrorResponse($"Mã OTP không đúng. Còn {attemptsLeft} lần thử");
        }

        // OTP verified successfully
        user.Status = UserState.PendingApproval.ToString();
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        user.OtpFailedCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendEmailVerifiedNotificationAsync(user.Email, user.FullName);

        return ApiResponse<bool>.SuccessResponse(true, "Xác minh email thành công! Tài khoản đang chờ Admin phê duyệt");
    }

    public async Task<ApiResponse<bool>> ResendOtpAsync(ResendOtpRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Email không tồn tại");

        if (user.Status != UserState.Unverified.ToString())
            return ApiResponse<bool>.ErrorResponse("Tài khoản không cần xác minh email");

        // Rate limit: must wait 60 seconds between OTP sends
        if (user.OtpExpiresAt != null)
        {
            var otpCreatedAt = user.OtpExpiresAt.Value.AddMinutes(-OtpExpiryMinutes);
            var timeSinceLastOtp = (DateTime.UtcNow - otpCreatedAt).TotalSeconds;
            if (timeSinceLastOtp < OtpResendCooldownSeconds)
            {
                var waitSeconds = (int)Math.Ceiling(OtpResendCooldownSeconds - timeSinceLastOtp);
                return ApiResponse<bool>.ErrorResponse($"Vui lòng đợi {waitSeconds} giây trước khi gửi lại mã OTP");
            }
        }

        var otp = GenerateOtp();
        user.OtpCode = HashOtp(otp);
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        user.OtpFailedCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendOtpEmailAsync(user.Email, otp, user.FullName);

        return ApiResponse<bool>.SuccessResponse(true, "Đã gửi lại mã OTP. Vui lòng kiểm tra email");
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, mã OTP đã được gửi");

        if (user.Status != UserState.Active.ToString() &&
            user.Status != UserState.PendingApproval.ToString())
            return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, mã OTP đã được gửi");

        if (user.OtpExpiresAt != null)
        {
            var otpCreatedAt = user.OtpExpiresAt.Value.AddMinutes(-OtpExpiryMinutes);
            var timeSinceLastOtp = (DateTime.UtcNow - otpCreatedAt).TotalSeconds;
            if (timeSinceLastOtp < OtpResendCooldownSeconds)
                return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, mã OTP đã được gửi");
        }

        var otp = GenerateOtp();
        user.OtpCode = HashOtp(otp);
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        user.OtpFailedCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await SendPasswordResetOtpEmailAsync(user.Email, otp, user.FullName);

        return ApiResponse<bool>.SuccessResponse(true, "Nếu email tồn tại, mã OTP đã được gửi");
    }

    public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Email không tồn tại");

        if (user.OtpFailedCount >= MaxOtpFailedAttempts)
            return ApiResponse<bool>.ErrorResponse("Nhập sai OTP quá nhiều lần. Vui lòng yêu cầu mã OTP mới");

        if (string.IsNullOrEmpty(user.OtpCode) || user.OtpExpiresAt == null || user.OtpExpiresAt < DateTime.UtcNow)
            return ApiResponse<bool>.ErrorResponse("Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới");

        if (user.OtpCode != HashOtp(request.OtpCode))
        {
            user.OtpFailedCount++;
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            var attemptsLeft = MaxOtpFailedAttempts - user.OtpFailedCount;
            return ApiResponse<bool>.ErrorResponse($"Mã OTP không đúng. Còn {attemptsLeft} lần thử");
        }

        // Reset password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.OtpCode = null;
        user.OtpExpiresAt = null;
        user.OtpFailedCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true, "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới");
    }

    public async Task<ApiResponse<AuthResponse>> GoogleLoginAsync(GoogleLoginRequest request, string? ipAddress = null, string? deviceInfo = null)
    {
        // Verify Google IdToken
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google IdToken");
            return Error("Google token không hợp lệ hoặc đã hết hạn");
        }

        var email = payload.Email;
        var fullName = payload.Name ?? payload.Email;
        var googleId = payload.Subject;

        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == email);

        if (user != null)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Existing user — link Google ID if not yet linked
                if (string.IsNullOrEmpty(user.GoogleId))
                {
                    user.GoogleId = googleId;
                    user.UpdateAt = DateTime.UtcNow;
                    userRepository.Update(user);
                }

                // Check if user can login
                if (user.Status == UserState.Unverified.ToString())
                {
                    // Auto-verify email since Google already verified it
                    user.Status = UserState.PendingApproval.ToString();
                    user.EmailVerifiedAt = DateTime.UtcNow;
                    user.OtpCode = null;
                    user.OtpExpiresAt = null;
                    user.UpdateAt = DateTime.UtcNow;
                    userRepository.Update(user);

                    await _unitOfWork.CommitTransactionAsync();

                    await SendEmailVerifiedNotificationAsync(user.Email, user.FullName);
                    return ApiResponse<AuthResponse>.SuccessWithMessage("Email đã xác minh thành công qua Google! Tài khoản đang chờ Admin phê duyệt");
                }

                if (user.Status == UserState.PendingApproval.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Error("Tài khoản đang chờ Admin phê duyệt. Vui lòng đợi thông báo qua email");
                }
                if (user.Status == UserState.Rejected.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Error("Tài khoản đã bị từ chối phê duyệt. Vui lòng liên hệ Admin");
                }
                if (user.Status == UserState.Banned.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Error("Tài khoản đã bị khóa vĩnh viễn bởi Admin");
                }
                if (user.Status == UserState.Deleted.ToString())
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return Error("Tài khoản đã bị xóa");
                }

                if (user.Status == UserState.Active.ToString())
                {
                    var roleName = await GetRoleName(user.RoleId);
                    var authResponse = await GenerateTokensAsync(user, roleName, ipAddress, deviceInfo);

                    await _unitOfWork.CommitTransactionAsync();

                    return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Đăng nhập Google thành công");
                }

                await _unitOfWork.RollbackTransactionAsync();
                return Error("Tài khoản không thể đăng nhập");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to complete Google login for user {Email}", email);
                return Error("Đăng nhập Google thất bại. Vui lòng thử lại sau");
            }
        }

        // Email chưa được đăng ký — chặn, không tự tạo tài khoản
        _logger.LogWarning("Google login attempt with unregistered email: {Email}", email);
        return Error("Email này chưa được đăng ký trong hệ thống. Vui lòng đăng ký tài khoản trước");
    }

    #endregion

    #region Login Helpers

    private async Task<ApiResponse<AuthResponse>?> ValidateAndHandleAccountStatus(User user, dynamic userRepository)
    {
        var status = user.Status;

        if (status == UserState.Unverified.ToString())
            return Error("Tài khoản chưa xác minh email. Vui lòng kiểm tra email để nhập mã OTP");

        if (status == UserState.PendingApproval.ToString())
            return Error("Tài khoản đang chờ Admin phê duyệt. Vui lòng đợi thông báo qua email");

        if (status == UserState.Rejected.ToString())
            return Error("Tài khoản đã bị từ chối phê duyệt. Vui lòng liên hệ Admin");

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

    private (bool IsUnlocked, int RemainingMinutes) TryAutoUnlock(User user)
    {
        var lockoutEndTime = user.UpdateAt.AddMinutes(LockoutDurationMinutes);
        var remainingTime = lockoutEndTime - DateTime.UtcNow;

        if (remainingTime > TimeSpan.Zero)
            return (false, (int)Math.Ceiling(remainingTime.TotalMinutes));

        // Auto-unlock
        user.Status = UserState.Active.ToString();
        user.FailedLoginCount = 0;
        user.UpdateAt = DateTime.UtcNow;
        return (true, 0);
    }

    private async Task<ApiResponse<AuthResponse>> HandleFailedLogin(User user, dynamic userRepository)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.FailedLoginCount++;
            user.UpdateAt = DateTime.UtcNow;

            if (user.FailedLoginCount >= MaxFailedLoginAttempts)
            {
                user.Status = UserState.Locked.ToString();
                userRepository.Update(user);
                await _unitOfWork.CommitTransactionAsync();
                return Error($"Tài khoản đã bị khóa tạm thời do đăng nhập sai quá {MaxFailedLoginAttempts} lần. Vui lòng thử lại sau {LockoutDurationMinutes} phút");
            }

            userRepository.Update(user);
            await _unitOfWork.CommitTransactionAsync();

            var attemptsLeft = MaxFailedLoginAttempts - user.FailedLoginCount;
            return Error($"Email hoặc mật khẩu không hợp lệ. Còn {attemptsLeft} lần thử");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to handle failed login for user {UserId}", user.UserId);
            return Error("Đăng nhập thất bại. Vui lòng thử lại sau");
        }
    }

    private static bool VerifyPassword(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);

    #endregion

    #region Registration Helpers

    private async Task<bool> EmailExists(string email)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

    private async Task<ApiResponse<AuthResponse>?> ValidatePublicRegistrationRole(int roleId)
    {
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(roleId);

        if (role == null)
            return Error("Loại đăng ký không hợp lệ");

        // Only Vendor and SupplierStaff can register publicly
        if (roleId != (int)RoleUser.Vendor && roleId != (int)RoleUser.SupplierStaff)
            return Error("Loại đăng ký này không được phép. Chỉ Vendor và SupplierStaff (nhân viên siêu thị) mới có thể đăng ký công khai.");

        return null;
    }

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

    private async Task<AuthResponse> GenerateTokensAsync(User user, string roleName, string? ipAddress, string? deviceInfo)
    {
        var refreshTokenString = GenerateRefreshTokenString();

        // Save refresh token to database
        var refreshToken = CreateRefreshTokenEntity(user.UserId, refreshTokenString, ipAddress, deviceInfo);
        await _unitOfWork.Repository<RefreshToken>().AddAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user, roleName, refreshTokenString);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string roleName, string refreshToken)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var accessToken = GenerateAccessToken(user, roleName, jwtSettings, expiresAt);

        // Load MarketStaffInfo if user is SupplierStaff
        MarketStaffInfoDto? marketStaffInfo = null;
        if (user.RoleId == (int)RoleUser.SupplierStaff)
        {
            marketStaffInfo = await GetMarketStaffInfoAsync(user.UserId);
        }

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = MapToUserResponse(user, roleName, marketStaffInfo)
        };
    }

    private static string GenerateRefreshTokenString()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

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

    private static UserResponseDto MapToUserResponse(User user, string roleName, MarketStaffInfoDto? marketStaffInfo = null) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        RoleName = roleName,
        RoleId = user.RoleId,
        Status = Enum.TryParse<UserState>(user.Status, out var status) ? status : UserState.Unverified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdateAt,
        MarketStaffInfo = marketStaffInfo
    };

    private async Task<MarketStaffInfoDto?> GetMarketStaffInfoAsync(Guid userId)
    {
        var marketStaff = await _unitOfWork.Repository<MarketStaff>()
            .FirstOrDefaultAsync(ms => ms.UserId == userId);

        if (marketStaff == null) return null;

        var supermarket = await _unitOfWork.Repository<Supermarket>()
            .FirstOrDefaultAsync(s => s.SupermarketId == marketStaff.SupermarketId);

        return new MarketStaffInfoDto
        {
            MarketStaffId = marketStaff.MarketStaffId,
            Position = marketStaff.Position ?? "Nhân viên",
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

    #endregion

    #region Common Helpers

    private async Task<string> GetRoleName(int roleId)
    {
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(roleId);
        return role?.RoleName ?? "User";
    }

    private static ApiResponse<AuthResponse> Error(string message)
        => ApiResponse<AuthResponse>.ErrorResponse(message);

    #endregion

    #region OTP Helpers

    private static string GenerateOtp()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000;
        return number.ToString();
    }

    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(bytes);
    }

    #endregion

    #region Email Templates

    private async Task SendOtpEmailAsync(string email, string otp, string fullName)
    {
        var subject = "CloseExp AI - Xác minh email của bạn";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0;'>CloseExp AI</h1>
                </div>
                <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                    <h2>Xin chào {fullName}!</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản. Vui lòng sử dụng mã OTP bên dưới để xác minh email:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; background: #4CAF50; color: white; padding: 15px 30px; border-radius: 8px;'>{otp}</span>
                    </div>
                    <p style='color: #666;'>Mã OTP có hiệu lực trong <strong>{OtpExpiryMinutes} phút</strong>.</p>
                    <p style='color: #999; font-size: 12px;'>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                </div>
            </body>
            </html>";

        try { await _emailService.SendEmailAsync(email, subject, body); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send OTP email to {Email}", email); }
    }

    private async Task SendPasswordResetOtpEmailAsync(string email, string otp, string fullName)
    {
        var subject = "CloseExp AI - Đặt lại mật khẩu";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0;'>CloseExp AI</h1>
                </div>
                <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                    <h2>Xin chào {fullName}!</h2>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Sử dụng mã OTP bên dưới:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; background: #FF5722; color: white; padding: 15px 30px; border-radius: 8px;'>{otp}</span>
                    </div>
                    <p style='color: #666;'>Mã OTP có hiệu lực trong <strong>{OtpExpiryMinutes} phút</strong>.</p>
                    <p style='color: #999; font-size: 12px;'>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                </div>
            </body>
            </html>";

        try { await _emailService.SendEmailAsync(email, subject, body); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send password reset email to {Email}", email); }
    }

    private async Task SendEmailVerifiedNotificationAsync(string email, string fullName)
    {
        var subject = "CloseExp AI - Email đã được xác minh!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0;'>✓ Email Đã Xác Minh</h1>
                </div>
                <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                    <h2>Xin chào {fullName}!</h2>
                    <p>Email của bạn đã được xác minh thành công! 🎉</p>
                    <p>Tài khoản của bạn hiện đang <strong>chờ Admin phê duyệt</strong>. Bạn sẽ nhận được thông báo qua email khi tài khoản được phê duyệt.</p>
                    <p style='color: #999; font-size: 12px;'>Cảm ơn bạn đã sử dụng CloseExp AI!</p>
                </div>
            </body>
            </html>";

        try { await _emailService.SendEmailAsync(email, subject, body); }
        catch (Exception ex) { _logger.LogError(ex, "Failed to send email verified notification to {Email}", email); }
    }

    #endregion
}
