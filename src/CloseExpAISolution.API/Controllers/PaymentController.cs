using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;

namespace CloseExpAISolution.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>Create PayOS payment link for an order (persists a <c>Transaction</c> row).</summary>
    [HttpPost("create-payment-link")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentRequestDto requestDto)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdString, out var userId))
            return Unauthorized("Invalid user token.");

        try
        {
            var checkoutUrl = await _paymentService.CreatePaymentLinkAsync(
                userId,
                requestDto.OrderId,
                requestDto.ReturnUrl,
                requestDto.CancelUrl);
            return Ok(new { CheckoutUrl = checkoutUrl });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already been paid", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Create payment link failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PayOS webhook. Use the full JSON envelope (<see cref="Webhook"/>) so the SDK can verify the signature.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandlePayOsWebhook([FromBody] Webhook webhookData)
    {
        try
        {
            await _paymentService.HandleWebhookAsync(webhookData);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PayOS webhook failed");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>Poll PayOS for link status and finalize local transaction + order if paid.</summary>
    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequestDto body)
    {
        try
        {
            if (!long.TryParse(body.OrderCode, out var orderCode))
                return BadRequest(new { success = false, message = "Invalid orderCode. Must be a numeric PayOS order code." });

            var result = await _paymentService.ConfirmPaymentAsync(orderCode);
            if (result.Success)
                return Ok(new { success = true });

            var payload = new
            {
                success = false,
                result.Message,
                errorCode = result.ErrorCode.ToString(),
                result.PayOsStatus,
                result.AmountPaid,
                result.Amount
            };

            return result.ErrorCode switch
            {
                PaymentConfirmErrorCode.TransactionMissing => NotFound(payload),
                _ => BadRequest(payload)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Confirm payment failed for order code {Code}", body.OrderCode);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
