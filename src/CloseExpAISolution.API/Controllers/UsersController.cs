using System.Security.Claims;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IServiceProviders _services;
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxImageSize = 5 * 1024 * 1024; // 5MB

    public UsersController(IServiceProviders services)
    {
        _services = services;
    }

    /// <summary>
    /// Get current user profile (Authenticated user)
    /// </summary>
    [HttpGet("current-user")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserResponseDto>.ErrorResponse("Không thể xác định người dùng"));
        }

        var result = await _services.UserService.GetUserByIdAsync(userId);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update current user profile (Authenticated user - cannot update status/role)
    /// </summary>
    [HttpPut("current-user")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserResponseDto>.ErrorResponse("Không thể xác định người dùng"));
        }

        var result = await _services.UserService.UpdateProfileAsync(userId, request);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy người dùng")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Soft delete current user account (Only Vendor can delete their own account, others must contact Admin)
    /// </summary>
    [HttpDelete("current-user")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyAccount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.UserService.DeleteOwnAccountAsync(userId.Value);
        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy người dùng")
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUsers()
    {
        var result = await _services.UserService.GetAllUsersAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get user by ID (Admin only)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var result = await _services.UserService.GetUserByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new user (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestDto request)
    {
        var result = await _services.UserService.CreateUserAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetUserById), new { id = result.Data?.UserId }, result);
    }

    /// <summary>
    /// Update an existing user (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequestDto request)
    {
        var result = await _services.UserService.UpdateUserAsync(id, request);

        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy người dùng")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a user (soft delete, Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var result = await _services.UserService.DeleteUserAsync(id);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Update user account status (Admin only)
    /// Used for verifying or banning user accounts
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">New status (Unverified, Verified, Banned, Deleted, Hidden)</param>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequestDto request)
    {
        var result = await _services.UserService.UpdateUserStatusAsync(id, request);

        if (!result.Success)
        {
            if (result.Message == "Không tìm thấy người dùng")
            {
                return NotFound(result);
            }
            return BadRequest(result);
        }

        return Ok(result);
    }

    #region User Image Endpoints

    /// <summary>
    /// Upload avatar/profile image for current user
    /// </summary>
    /// <param name="file">Image file (JPEG, PNG, GIF, WebP - max 5MB)</param>
    /// <param name="imageType">Type of image: avatar, cover, etc. Default: avatar</param>
    /// <param name="setAsPrimary">Set this image as primary profile picture</param>
    [HttpPost("current-user/images")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserImageResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserImageResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadMyImage(
        IFormFile file,
        [FromQuery] string imageType = "avatar",
        [FromQuery] bool setAsPrimary = true)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<UserImageResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var validationError = ValidateImageFile(file);
        if (validationError != null)
            return BadRequest(ApiResponse<UserImageResponseDto>.ErrorResponse(validationError));

        await using var stream = file.OpenReadStream();
        var userImage = await _services.UserImageService.UploadAsync(
            stream, file.FileName, file.ContentType, userId.Value, imageType, setAsPrimary);

        var response = MapToUserImageResponse(userImage);
        return Created(string.Empty, ApiResponse<UserImageResponseDto>.SuccessResponse(response, "Tải ảnh lên thành công"));
    }

    /// <summary>
    /// Get all images of current user
    /// </summary>
    [HttpGet("current-user/images")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserImageResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyImages()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IEnumerable<UserImageResponseDto>>.ErrorResponse("Không thể xác định người dùng"));

        var images = await _services.UserImageService.GetByUserIdAsync(userId.Value);
        var response = images.Select(MapToUserImageResponse);

        return Ok(ApiResponse<IEnumerable<UserImageResponseDto>>.SuccessResponse(response));
    }

    /// <summary>
    /// Get primary/avatar image of current user
    /// </summary>
    [HttpGet("current-user/images/primary")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserImageResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserImageResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyPrimaryImage()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<UserImageResponseDto>.ErrorResponse("Không thể xác định người dùng"));

        var image = await _services.UserImageService.GetPrimaryAsync(userId.Value);
        if (image == null)
            return NotFound(ApiResponse<UserImageResponseDto>.ErrorResponse("Không tìm thấy ảnh đại diện"));

        return Ok(ApiResponse<UserImageResponseDto>.SuccessResponse(MapToUserImageResponse(image)));
    }

    /// <summary>
    /// Set an image as primary profile picture
    /// </summary>
    [HttpPatch("current-user/images/{imageId:guid}/set-primary")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetMyPrimaryImage(Guid imageId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));

        var result = await _services.UserImageService.SetPrimaryAsync(userId.Value, imageId);
        if (!result)
            return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy ảnh hoặc ảnh không thuộc về bạn"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Đặt ảnh đại diện thành công"));
    }

    /// <summary>
    /// Delete an image of current user
    /// </summary>
    [HttpDelete("current-user/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMyImage(Guid imageId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Không thể xác định người dùng"));

        // Verify image belongs to user
        var images = await _services.UserImageService.GetByUserIdAsync(userId.Value);
        if (!images.Any(i => i.ImageId == imageId))
            return NotFound(ApiResponse<bool>.ErrorResponse("Không tìm thấy ảnh hoặc ảnh không thuộc về bạn"));

        var result = await _services.UserImageService.DeleteAsync(imageId);
        if (!result)
            return NotFound(ApiResponse<bool>.ErrorResponse("Xóa ảnh thất bại"));

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Xóa ảnh thành công"));
    }

    /// <summary>
    /// Get images of a user by ID (Admin only)
    /// </summary>
    [HttpGet("{userId:guid}/images")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserImageResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserImages(Guid userId)
    {
        var images = await _services.UserImageService.GetByUserIdAsync(userId);
        var response = images.Select(MapToUserImageResponse);
        return Ok(ApiResponse<IEnumerable<UserImageResponseDto>>.SuccessResponse(response));
    }

    #endregion

    #region Private Helpers

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? ValidateImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Vui lòng chọn file ảnh";

        if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
            return "Chỉ chấp nhận file ảnh: JPEG, PNG, GIF, WebP";

        if (file.Length > MaxImageSize)
            return $"File ảnh không được vượt quá {MaxImageSize / 1024 / 1024}MB";

        return null;
    }

    private UserImageResponseDto MapToUserImageResponse(Domain.Entities.UserImage image)
    {
        return new UserImageResponseDto
        {
            ImageId = image.ImageId,
            UserId = image.UserId,
            ImageUrl = image.ImageUrl,
            PreSignedUrl = _services.R2StorageService.GetPreSignedUrlForImage(image.ImageUrl, TimeSpan.FromHours(1)),
            ImageType = image.ImageType,
            IsPrimary = image.IsPrimary,
            UploadedAt = image.UploadedAt
        };
    }

    #endregion
}
