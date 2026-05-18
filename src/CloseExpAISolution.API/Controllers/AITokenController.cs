using CloseExpAISolution.API.Helpers;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.DTOs.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Authorize(Roles = "SupermarketStaff")]
[Route("api/[controller]")]
[Produces("application/json")]
public class AITokenController : ControllerBase
{
    private readonly IAIServiceClient _aiServiceClient;
    private readonly ILogger<AITokenController> _logger;

    public AITokenController(
        IAIServiceClient aiServiceClient,
        ILogger<AITokenController> logger)
    {
        _aiServiceClient = aiServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Get current month token usage for all AI features for the logged-in user.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetAllTokenStatus(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
    {
        var userId = StaffClaimsParser.ReadUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var usage = await _aiServiceClient.GetAllTokenUsageAsync(
                userId.Value.ToString(),
                month,
                cancellationToken);

            if (usage == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.ErrorResponse("Unable to retrieve token status from AI service"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(usage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all token status for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.ErrorResponse("AI service unavailable"));
        }
    }

    /// <summary>
    /// Get current month token usage for a specific feature (ocr | pricing) for the logged-in user.
    /// </summary>
    [HttpGet("status/{feature}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetFeatureTokenStatus(
        string feature,
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
    {
        var userId = StaffClaimsParser.ReadUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        var validFeatures = new[] { "ocr", "pricing" };
        if (!validFeatures.Contains(feature.ToLower()))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                $"Invalid feature '{feature}'. Valid values: {string.Join(", ", validFeatures)}"));
        }

        try
        {
            var usage = await _aiServiceClient.GetTokenUsageAsync(
                userId.Value.ToString(),
                feature.ToLower(),
                month,
                cancellationToken);

            if (usage == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.ErrorResponse("Unable to retrieve token status from AI service"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(usage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token status for feature {Feature}, user {UserId}", feature, userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.ErrorResponse("AI service unavailable"));
        }
    }

    /// <summary>
    /// Get full token usage history across all months for the logged-in user.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetTokenHistory(CancellationToken cancellationToken = default)
    {
        var userId = StaffClaimsParser.ReadUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("User not authenticated"));
        }

        try
        {
            var history = await _aiServiceClient.GetTokenHistoryAsync(
                userId.Value.ToString(),
                cancellationToken);

            if (history == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.ErrorResponse("Unable to retrieve token history from AI service"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(history));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token history for user {UserId}", userId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.ErrorResponse("AI service unavailable"));
        }
    }

    /// <summary>
    /// Get token budget configuration (monthly limits and per-call costs).
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetTokenConfig(CancellationToken cancellationToken = default)
    {
        try
        {
            var config = await _aiServiceClient.GetTokenConfigAsync(cancellationToken);

            if (config == null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    ApiResponse<object>.ErrorResponse("Unable to retrieve token config from AI service"));
            }

            return Ok(ApiResponse<object>.SuccessResponse(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token config");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<object>.ErrorResponse("AI service unavailable"));
        }
    }
}
