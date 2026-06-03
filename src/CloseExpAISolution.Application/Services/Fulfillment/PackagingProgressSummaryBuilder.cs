using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Fulfillment;

/// <summary>
/// Builds Vietnamese order-level packaging progress text from line items.
/// </summary>
public static class PackagingProgressSummaryBuilder
{
    public static string Build(IReadOnlyList<OrderItem> items)
    {
        if (items.Count == 0)
            return "—";

        var done = items.Count(i => i.PackagingStatus == PackagingState.Completed);
        var failed = items.Count(i => i.PackagingStatus == PackagingState.Failed);
        var open = items.Count - done - failed;

        if (open > 0)
            return $"{done}/{items.Count} dòng đã đóng gói xong, {open} dòng đang xử lý";

        return failed == 0
            ? "Tất cả dòng đã đóng gói xong"
            : $"{done} thành công, {failed} thất bại";
    }
}
