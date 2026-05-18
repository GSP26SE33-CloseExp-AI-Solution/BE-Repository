using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IServiceProviders _services;

    public CustomersController(IServiceProviders services)
    {
        _services = services;
    }

    [Authorize]
    [HttpGet("me/addresses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CustomerAddressDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CustomerAddressDto>>>> GetMyAddresses(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Không thể xác định người dùng từ token"));
        }

        var addresses = await _services.OrderService.GetCustomerAddressesByUserIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<CustomerAddressDto>>.SuccessResponse(addresses));
    }

    [HttpGet("products/{productId:guid}/purchase-units")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductPurchaseUnitDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductPurchaseUnitDto>>>> GetProductPurchaseUnits(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var units = await _services.ProductService
                .GetPurchaseUnitsForProductAsync(productId, cancellationToken);

            return Ok(ApiResponse<IReadOnlyList<ProductPurchaseUnitDto>>.SuccessResponse(
                units,
                units.Count == 0
                    ? "Sản phẩm chưa có đơn vị mua khả dụng"
                    : $"Tìm thấy {units.Count} đơn vị mua"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("stocklots/available")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AvailableStocklotDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AvailableStocklotDto>>>> GetAvailableStockLots(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var (items, totalCount) = await _services.ProductService
            .GetAvailableStockLotsForCustomerAsync(pageNumber, pageSize, cancellationToken);

        var result = new PaginatedResult<AvailableStocklotDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<AvailableStocklotDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô hàng khả dụng"));
    }

    [HttpGet("products/search-by-ai")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AvailableStocklotDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AvailableStocklotDto>>>> SearchAvailableStockLotsWithAi(
        [FromQuery] string description,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Mô tả tìm kiếm không được để trống."));
        }

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var (items, totalCount) = await _services.ProductService
            .SearchAvailableStockLotsWithAiAsync(description, pageNumber, pageSize, cancellationToken);

        var result = new PaginatedResult<AvailableStocklotDto>
        {
            Items = items,
            TotalResult = totalCount,
            Page = pageNumber,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResult<AvailableStocklotDto>>.SuccessResponse(
            result,
            $"Tìm thấy {totalCount} lô hàng khả dụng dựa trên mô tả AI"));
    }
}
