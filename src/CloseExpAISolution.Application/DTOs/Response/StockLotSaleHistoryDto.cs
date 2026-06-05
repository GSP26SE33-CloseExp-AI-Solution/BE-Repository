namespace CloseExpAISolution.Application.DTOs.Response;

public class StockLotSaleHistorySummaryDto
{
    public int ConfirmedOrderCount { get; set; }
    public int TotalSaleLines { get; set; }
    public decimal TotalQuantityInLotUnit { get; set; }
    public decimal TotalRevenue { get; set; }
    public string LotUnitName { get; set; } = string.Empty;
    public string? LotUnitSymbol { get; set; }
}

public class StockLotSaleHistoryItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string OrderStatusText { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string? CustomerName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? PurchaseUnitName { get; set; }
    public string? PurchaseUnitSymbol { get; set; }
    public decimal QuantityInLotUnit { get; set; }
}

public class StockLotSaleHistoryResponseDto
{
    public StockLotSaleHistorySummaryDto Summary { get; set; } = new();
    public IEnumerable<StockLotSaleHistoryItemDto> Items { get; set; } = Array.Empty<StockLotSaleHistoryItemDto>();
    public int TotalResult { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
