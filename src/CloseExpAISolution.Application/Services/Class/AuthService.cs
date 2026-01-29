using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var user = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Email hoặc mật khẩu không hợp lệ");
        }

        // Check if account is locked
        if (user.Status == UserState.Banned.ToString())
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần");
        }

        if (user.Status == UserState.Deleted.ToString())
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã bị xóa");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Increment failed login count
            user.FailedLoginCount++;

            if (user.FailedLoginCount >= MaxFailedLoginAttempts)
            {
                user.Status = UserState.Banned.ToString();
                userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return ApiResponse<AuthResponse>.ErrorResponse("Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần");
            }

            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<AuthResponse>.ErrorResponse($"Email hoặc mật khẩu không hợp lệ. Còn {MaxFailedLoginAttempts - user.FailedLoginCount} lần thử");
        }

        // Reset failed login count on successful login
        user.FailedLoginCount = 0;
        user.Status = UserState.Active.ToString();
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        // Get role
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(user.RoleId);

        // Generate tokens
        var authResponse = GenerateTokens(user, role?.RoleName ?? "User");

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Đăng nhập thành công");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();

        // Check if email already exists
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Email đã được đăng ký");
        }

        // Map RegistrationType to RoleId
        var roleId = (int)request.RegistrationType;

        // Verify role exists and is allowed for public registration
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Loại đăng ký không hợp lệ");
        }

        // Double-check that only Vendor and MarketStaff can register publicly
        if (roleId != (int)RoleUser.Vendor && roleId != (int)RoleUser.MarketStaff)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Loại đăng ký này không được phép. Chỉ Vendor và MarketStaff mới có thể đăng ký công khai.");
        }

        // Create new user
        var user = new User
        {
            UserId = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = roleId,
            Status = UserState.Active.ToString(),
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Generate tokens
        var authResponse = GenerateTokens(user, role.RoleName);

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Đăng ký thành công");
    }

    private AuthResponse GenerateTokens(User user, string roleName)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "60");
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

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

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Guid.NewGuid().ToString("N");

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = roleName,
                RoleId = user.RoleId,
                Status = Enum.TryParse<UserState>(user.Status, out var status) ? status : UserState.Active,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdateAt
            }
        };
    }
}
