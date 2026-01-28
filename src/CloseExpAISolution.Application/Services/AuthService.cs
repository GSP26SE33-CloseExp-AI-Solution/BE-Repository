using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CloseExpAISolution.Application.Services;

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
            return ApiResponse<AuthResponse>.ErrorResponse("Invalid email or password");
        }

        // Check if account is locked
        if (user.Status == UserState.Banned.ToString())
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Account has been locked due to too many failed login attempts");
        }

        if (user.Status == UserState.Deleted.ToString())
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Account has been deleted");
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
                return ApiResponse<AuthResponse>.ErrorResponse("Account has been locked due to too many failed login attempts");
            }

            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<AuthResponse>.ErrorResponse($"Invalid email or password. {MaxFailedLoginAttempts - user.FailedLoginCount} attempts remaining");
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

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Login successful");
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var userRepository = _unitOfWork.Repository<User>();

        // Check if email already exists
        var existingUser = await userRepository.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Email already registered");
        }

        // Verify role exists
        var roleRepository = _unitOfWork.Repository<Role>();
        var role = await roleRepository.GetByIdAsync(request.RoleId);
        if (role == null)
        {
            return ApiResponse<AuthResponse>.ErrorResponse("Invalid role");
        }

        // Create new user
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

        // Generate tokens
        var authResponse = GenerateTokens(user, role.RoleName);

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Registration successful");
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
        var refreshToken = Guid.NewGuid().ToString("N"); // Simple refresh token for now

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new UserResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = roleName,
                RoleId = user.RoleId,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdateAt
            }
        };
    }
}
