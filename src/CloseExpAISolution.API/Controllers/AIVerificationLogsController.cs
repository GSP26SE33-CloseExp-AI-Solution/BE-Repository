using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.ServiceProviders;
using CloseExpAISolution.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIVerificationLogsController : ControllerBase
{
    private readonly IServiceProviders _services;

    public AIVerificationLogsController(IServiceProviders services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<AIVerificationLog>>>> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var items = (await _services.AIVerificationLogService.GetAllAsync()).ToList();
        var total = items.Count;
        var pageItems = items.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var result = new PaginatedResult<AIVerificationLog>
        {
            Items = pageItems,
            TotalResult = total,
            Page = pageNumber,
            PageSize = pageSize
        };
        return Ok(ApiResponse<PaginatedResult<AIVerificationLog>>.SuccessResponse(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AIVerificationLog>>> GetById(Guid id)
    {
        var item = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (item == null) return NotFound(ApiResponse<AIVerificationLog>.ErrorResponse("Không tìm thấy nhật ký xác minh AI"));
        return Ok(ApiResponse<AIVerificationLog>.SuccessResponse(item));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AIVerificationLog>>> Create([FromBody] AIVerificationLog input, CancellationToken cancellationToken)
    {
        var created = await _services.AIVerificationLogService.AddAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.VerificationId }, ApiResponse<AIVerificationLog>.SuccessResponse(created, "Tạo thành công"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] AIVerificationLog input, CancellationToken cancellationToken)
    {
        var existing = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy nhật ký xác minh AI"));

        input.VerificationId = id;
        await _services.AIVerificationLogService.UpdateAsync(input, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Cập nhật thành công"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _services.AIVerificationLogService.FirstOrDefaultAsync(x => x.VerificationId == id);
        if (existing == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy nhật ký xác minh AI"));

        await _services.AIVerificationLogService.DeleteAsync(existing, cancellationToken);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "Xóa thành công"));
    }
}
