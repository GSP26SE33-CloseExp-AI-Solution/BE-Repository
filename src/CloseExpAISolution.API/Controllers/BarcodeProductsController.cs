using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.DTOs.Request;
using Microsoft.AspNetCore.Mvc;

namespace CloseExpAISolution.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BarcodeProductsController : ControllerBase
{
    private readonly IBarcodeLookupService _barcodeLookupService;
    public BarcodeProductsController(
        IBarcodeLookupService barcodeLookupService)
    {
        _barcodeLookupService = barcodeLookupService;
    }

    [HttpGet("lookup/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BarcodeProductInfo>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BarcodeProductInfo>>> Lookup(string barcode)
    {
        var result = await _barcodeLookupService.LookupAsync(barcode);

        if (result == null)
        {
            return NotFound(ApiResponse<BarcodeProductInfo>.ErrorResponse(
                $"Không tìm thấy sản phẩm với mã vạch {barcode}"));
        }

        return Ok(ApiResponse<BarcodeProductInfo>.SuccessResponse(result));
    }

    [HttpPost("lookup/batch")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, BarcodeProductInfo?>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Dictionary<string, BarcodeProductInfo?>>>> LookupBatch([FromBody] BatchLookupRequest request)
    {
        if (request.Barcodes == null || !request.Barcodes.Any())
        {
            return BadRequest(ApiResponse<Dictionary<string, BarcodeProductInfo?>>.ErrorResponse("Danh sách barcode không được rỗng"));
        }

        var results = await _barcodeLookupService.LookupBatchAsync(request.Barcodes);
        return Ok(ApiResponse<Dictionary<string, BarcodeProductInfo?>>.SuccessResponse(results));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<BarcodeProductInfo>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<BarcodeProductInfo>>>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<IEnumerable<BarcodeProductInfo>>.ErrorResponse("Từ khóa tìm kiếm là bắt buộc"));
        }

        var results = await _barcodeLookupService.SearchAsync(q, limit);
        return Ok(ApiResponse<IEnumerable<BarcodeProductInfo>>.SuccessResponse(results));
    }

    [HttpGet("is-vietnamese/{barcode}")]
    [ProducesResponseType(typeof(ApiResponse<VietnameseBarcodeResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<VietnameseBarcodeResponse>> IsVietnamese(string barcode)
    {
        var isVietnamese = _barcodeLookupService.IsVietnameseBarcode(barcode);
        return Ok(ApiResponse<VietnameseBarcodeResponse>.SuccessResponse(new VietnameseBarcodeResponse
        {
            Barcode = barcode,
            IsVietnamese = isVietnamese,
            Message = isVietnamese
                ? "Đây là mã vạch sản phẩm Việt Nam (prefix 893)"
                : "Đây không phải mã vạch sản phẩm Việt Nam"
        }));
    }
}
