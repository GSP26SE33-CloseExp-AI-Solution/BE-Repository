using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SupermarketsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public SupermarketsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<SupermarketResponseDto>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.SupermarketService.GetAllWithDtoAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<SupermarketResponseDto>
        {
            Items = pageItems,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<SupermarketResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SupermarketResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableSupermarkets()
    {
        var availableSupermarkets = await _services.SupermarketService.GetAvailableWithDtoAsync();
        return Ok(ApiResponse<IEnumerable<SupermarketResponseDto>>.SuccessResponse(availableSupermarkets));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SupermarketResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var searchResults = await _services.SupermarketService.SearchAsync(query);
        return Ok(ApiResponse<IEnumerable<SupermarketResponseDto>>.SuccessResponse(searchResults));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SupermarketResponseDto>>> GetById(Guid id)
    {
        var item = await _services.SupermarketService.GetByIdWithDtoAsync(id);
        if (item == null) return NotFound(ApiResponse<SupermarketResponseDto>.ErrorResponse("Không tìm thấy siêu thị"));
        return Ok(ApiResponse<SupermarketResponseDto>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SupermarketResponseDto>>> Create([FromBody] CreateSupermarketRequestDto request, CancellationToken cancellationToken)
    {
        var created = await _services.SupermarketService.CreateSupermarketAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.SupermarketId }, ApiResponse<SupermarketResponseDto>.SuccessResponse(created, "Tạo thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateSupermarketRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            await _services.SupermarketService.UpdateSupermarketAsync(id, request, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy siêu thị"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _services.SupermarketService.DeleteSupermarketAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa thành công"));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy siêu thị"));
        }
    }
}
