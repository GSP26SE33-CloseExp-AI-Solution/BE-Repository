using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

/// <summary>
/// Service for importing products from Excel files.
/// Supports flexible column mapping to handle different Excel formats from various supermarkets.
/// </summary>
public interface IExcelImportService
{
    /// <summary>
    /// Preview Excel file - get columns and sample data for mapping UI.
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="headerRow">Row index containing headers (0-based)</param>
    /// <param name="previewRows">Number of data rows to preview</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview data with suggested mappings</returns>
    Task<ExcelPreviewResponseDto> PreviewExcelAsync(
        Stream fileStream,
        int headerRow = 0,
        int previewRows = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Import products from Excel file using user-defined column mappings.
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <param name="request">Import request with column mappings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Import result with success/error counts</returns>
    Task<ExcelImportResponseDto> ImportProductsAsync(
        Stream fileStream,
        ExcelImportRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of available system fields that can be mapped.
    /// </summary>
    IEnumerable<string> GetAvailableFields();

    /// <summary>
    /// Get suggested mapping based on Excel column name.
    /// </summary>
    /// <param name="excelColumnName">Column name from Excel</param>
    /// <returns>Suggested system field name or null</returns>
    string? SuggestFieldMapping(string excelColumnName);
}
