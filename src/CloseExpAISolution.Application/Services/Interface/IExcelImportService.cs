using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IExcelImportService
{
    Task<ExcelPreviewResponseDto> PreviewExcelAsync(
        Stream fileStream,
        int headerRow = 0,
        int previewRows = 5,
        CancellationToken cancellationToken = default);
    Task<ExcelImportResponseDto> ImportProductsAsync(
        Stream fileStream,
        ExcelImportRequestDto request,
        CancellationToken cancellationToken = default); IEnumerable<string> GetAvailableFields();
    string? SuggestFieldMapping(string excelColumnName);
}