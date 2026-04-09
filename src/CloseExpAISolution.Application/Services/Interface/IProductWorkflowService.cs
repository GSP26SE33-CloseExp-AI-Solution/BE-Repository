using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IProductWorkflowService
{
    Task<ScanBarcodeResponseDto> ScanBarcodeAsync(
        string barcode,
        Guid supermarketId,
        CancellationToken cancellationToken = default);

    Task<StaffProductIdentificationResponseDto> IdentifyProductForStaffAsync(
        string barcode,
        Guid supermarketId,
        CancellationToken cancellationToken = default);

    Task<StockLotResponseDto> CreateStockLotFromExistingAsync(
        CreateStockLotFromExistingDto request,
        CancellationToken cancellationToken = default);

    Task<OcrAnalysisResponseDto> AnalyzeProductImageAsync(
        Guid supermarketId,
        Stream imageStream,
        string fileName,
        string contentType,
        bool skipAi = false,
        CancellationToken cancellationToken = default);

    Task<CreateNewProductResponseDto> CreateNewProductAsync(
        CreateNewProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task<CreateNewProductResponseDto> CreateProductFromStaffWorkflowAsync(
        StaffCreateProductFromWorkflowRequestDto request,
        Guid supermarketId,
        string staffName,
        CancellationToken cancellationToken = default);

    Task<ProductResponseDto> VerifyProductAsync(
        Guid productId,
        VerifyProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task<PricingSuggestionResponseDto> GetLotPricingSuggestionAsync(
        Guid lotId,
        GetPricingSuggestionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<StockLotResponseDto> ConfirmLotPriceAsync(
        Guid lotId,
        ConfirmPriceRequestDto request,
        CancellationToken cancellationToken = default);

    Task<StockLotResponseDto> PublishStockLotAsync(
        Guid lotId,
        PublishProductRequestDto request,
        CancellationToken cancellationToken = default);

    Task<StaffCreateLotAndPublishResponseDto> CreateLotAndPublishForStaffAsync(
        StaffCreateLotAndPublishRequestDto request,
        Guid supermarketId,
        string staffName,
        CancellationToken cancellationToken = default);

    Task<ProductResponseDto?> GetProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<StockLotResponseDto?> GetStockLotAsync(
        Guid lotId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProductResponseDto>> GetProductsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<StockLotResponseDto>> GetStockLotsByStatusAsync(
        Guid supermarketId,
        ProductState status,
        CancellationToken cancellationToken = default);

    Task<WorkflowSummaryDto> GetWorkflowSummaryAsync(
        Guid supermarketId,
        CancellationToken cancellationToken = default);

}

