using System.Globalization;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace CloseExpAISolution.Application.Services.Class;

public class ExcelImportService : IExcelImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExcelImportService> _logger;

    public ExcelImportService(
        IUnitOfWork unitOfWork,
        ILogger<ExcelImportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        // Set EPPlus license context (required for EPPlus 5+)
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public Task<ExcelPreviewResponseDto> PreviewExcelAsync(
        Stream fileStream,
        int headerRow = 0,
        int previewRows = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Previewing Excel file with headerRow={HeaderRow}, previewRows={PreviewRows}", headerRow, previewRows);

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null)
        {
            throw new InvalidOperationException("Excel file contains no worksheets");
        }

        var response = new ExcelPreviewResponseDto();
        var dimension = worksheet.Dimension;

        if (dimension == null)
        {
            throw new InvalidOperationException("Excel worksheet is empty");
        }

        response.TotalRows = dimension.Rows - headerRow - 1; // Exclude header

        // Get column names from header row
        for (int col = 1; col <= dimension.Columns; col++)
        {
            var cellValue = worksheet.Cells[headerRow + 1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(cellValue))
            {
                response.Columns.Add(cellValue);

                // Try to suggest mapping
                var suggestedField = SuggestFieldMapping(cellValue);
                if (suggestedField != null)
                {
                    response.SuggestedMappings.Add(new ExcelColumnMappingDto
                    {
                        ExcelColumn = cellValue,
                        SystemField = suggestedField
                    });
                }
            }
        }

        // Get preview data rows
        var dataStartRow = headerRow + 2; // 1-based, after header
        var endRow = Math.Min(dataStartRow + previewRows - 1, dimension.Rows);

        for (int row = dataStartRow; row <= endRow; row++)
        {
            var rowData = new Dictionary<string, string>();
            for (int col = 0; col < response.Columns.Count; col++)
            {
                var cellValue = worksheet.Cells[row, col + 1].Value?.ToString() ?? "";
                rowData[response.Columns[col]] = cellValue;
            }
            response.PreviewData.Add(rowData);
        }

        return Task.FromResult(response);
    }

    public async Task<ExcelImportResponseDto> ImportProductsAsync(
        Stream fileStream,
        ExcelImportRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Importing products from Excel for supermarket {SupermarketId}", request.SupermarketId);

        // Validate supermarket exists
        var supermarket = await _unitOfWork.SupermarketRepository.FirstOrDefaultAsync(
            s => s.SupermarketId == request.SupermarketId);
        if (supermarket == null)
        {
            throw new ArgumentException($"Supermarket {request.SupermarketId} not found");
        }

        // Get default unit
        var units = await _unitOfWork.Repository<UnitOfMeasure>().GetAllAsync();
        var defaultUnit = units.FirstOrDefault();
        if (defaultUnit == null)
        {
            throw new InvalidOperationException("No Unit found in database");
        }

        var response = new ExcelImportResponseDto();

        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();

        if (worksheet == null || worksheet.Dimension == null)
        {
            throw new InvalidOperationException("Excel file is empty");
        }

        var dimension = worksheet.Dimension;
        response.TotalRows = dimension.Rows - request.DataStartRow;

        // Build column index map
        var columnIndexMap = new Dictionary<string, int>();
        for (int col = 1; col <= dimension.Columns; col++)
        {
            var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                columnIndexMap[headerValue] = col;
            }
        }

        // Process each data row
        for (int row = request.DataStartRow + 1; row <= dimension.Rows; row++)
        {
            try
            {
                var rowData = ExtractRowData(worksheet, row, columnIndexMap, request.ColumnMappings);

                // Validate required fields
                if (!rowData.TryGetValue(ProductFieldNames.Name, out var name) || string.IsNullOrEmpty(name))
                {
                    throw new InvalidOperationException("Product name is required");
                }

                // Check if product with barcode already exists
                string? barcode = null;
                rowData.TryGetValue(ProductFieldNames.Barcode, out barcode);

                if (!string.IsNullOrEmpty(barcode))
                {
                    var existingProduct = await _unitOfWork.ProductRepository.FirstOrDefaultAsync(
                        p => p.Barcode == barcode);
                    if (existingProduct != null)
                    {
                        response.SkippedCount++;
                        response.Errors.Add(new ExcelImportErrorDto
                        {
                            RowNumber = row,
                            ErrorMessage = $"Product with barcode {barcode} already exists",
                            RowData = rowData
                        });
                        continue;
                    }
                }

                // Create product
                var categoryName = GetValueOrDefault(rowData, ProductFieldNames.Category);
                var category = string.IsNullOrWhiteSpace(categoryName)
                    ? null
                    : await _unitOfWork.Repository<Category>().FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == categoryName.ToLower());

                var product = new Product
                {
                    ProductId = Guid.NewGuid(),
                    SupermarketId = request.SupermarketId,
                    Name = name,
                    Barcode = barcode ?? "",
                    CategoryId = category?.CategoryId,
                    Sku = GetValueOrDefault(rowData, ProductFieldNames.Sku),
                    CreatedBy = request.ImportedBy,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = ProductState.Verified,
                    VerifiedBy = request.ImportedBy,
                    VerifiedAt = DateTime.UtcNow
                };

                await _unitOfWork.ProductRepository.AddAsync(product);

                var detail = new ProductDetail
                {
                    ProductDetailId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    Brand = GetValueOrDefault(rowData, ProductFieldNames.Brand),
                    Ingredients = GetValueOrDefault(rowData, ProductFieldNames.Ingredients),
                    Manufacturer = GetValueOrDefault(rowData, ProductFieldNames.Manufacturer),
                    Origin = GetValueOrDefault(rowData, ProductFieldNames.Origin),
                    Description = GetValueOrDefault(rowData, ProductFieldNames.Description),
                    StorageInstructions = GetValueOrDefault(rowData, ProductFieldNames.StorageInstructions),
                    UsageInstructions = GetValueOrDefault(rowData, ProductFieldNames.UsageInstructions)
                };
                await _unitOfWork.Repository<ProductDetail>().AddAsync(detail);

                response.CreatedProductIds.Add(product.ProductId);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error importing row {Row}", row);
                response.ErrorCount++;

                var rowData = new Dictionary<string, string>();
                for (int col = 1; col <= Math.Min(dimension.Columns, 10); col++)
                {
                    rowData[$"Column{col}"] = worksheet.Cells[row, col].Value?.ToString() ?? "";
                }

                response.Errors.Add(new ExcelImportErrorDto
                {
                    RowNumber = row,
                    ErrorMessage = ex.Message,
                    RowData = rowData
                });

                if (!request.SkipErrorRows)
                {
                    throw;
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Excel import completed: {Success} success, {Skipped} skipped, {Errors} errors",
            response.SuccessCount, response.SkippedCount, response.ErrorCount);

        return response;
    }

    public IEnumerable<string> GetAvailableFields()
    {
        return ProductFieldNames.AllFields;
    }

    public string? SuggestFieldMapping(string excelColumnName)
    {
        if (string.IsNullOrEmpty(excelColumnName))
            return null;

        var lowerColumn = excelColumnName.ToLowerInvariant().Trim();

        // Check against common Vietnamese names
        foreach (var mapping in ProductFieldNames.CommonVietnameseNames)
        {
            foreach (var pattern in mapping.Value)
            {
                if (lowerColumn.Contains(pattern) || pattern.Contains(lowerColumn))
                {
                    return mapping.Key;
                }
            }
        }

        // Check direct field name match
        foreach (var field in ProductFieldNames.AllFields)
        {
            if (lowerColumn.Contains(field.ToLowerInvariant()))
            {
                return field;
            }
        }

        return null;
    }

    private Dictionary<string, string> ExtractRowData(
        ExcelWorksheet worksheet,
        int row,
        Dictionary<string, int> columnIndexMap,
        List<ExcelColumnMappingDto> mappings)
    {
        var result = new Dictionary<string, string>();

        foreach (var mapping in mappings)
        {
            if (columnIndexMap.TryGetValue(mapping.ExcelColumn, out var colIndex))
            {
                var cellValue = worksheet.Cells[row, colIndex].Value?.ToString() ?? "";

                // Apply transform rule if specified
                if (!string.IsNullOrEmpty(mapping.TransformRule))
                {
                    cellValue = ApplyTransform(cellValue, mapping.TransformRule);
                }

                result[mapping.SystemField] = cellValue;
            }
        }

        return result;
    }

    private string ApplyTransform(string value, string transformRule)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(transformRule))
            return value;

        var rule = transformRule.ToLowerInvariant();

        if (rule == "toupper")
            return value.ToUpperInvariant();

        if (rule == "tolower")
            return value.ToLowerInvariant();

        if (rule == "trim")
            return value.Trim();

        if (rule.StartsWith("parsedate:"))
        {
            var format = transformRule.Substring(10);
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date.ToString("yyyy-MM-dd");
            }
        }

        return value;
    }

    private static string GetValueOrDefault(Dictionary<string, string> dict, string key, string defaultValue = "")
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var lower = value.ToLowerInvariant().Trim();
        return lower == "true" || lower == "1" || lower == "yes" || lower == "có" || lower == "co" || lower == "x";
    }
}
