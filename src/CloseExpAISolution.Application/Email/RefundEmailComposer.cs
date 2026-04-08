using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Email;

public static class RefundEmailComposer
{
    public static (string Subject, string BodyHtml) Compose(Refund refund, Order order, RefundNotificationKind kind)
    {
        return kind switch
        {
            RefundNotificationKind.Pending => BuildPending(refund, order),
            RefundNotificationKind.Approved => BuildApproved(refund, order),
            RefundNotificationKind.Rejected => BuildRejected(refund, order),
            RefundNotificationKind.Completed => BuildCompleted(refund, order),
            _ => BuildPending(refund, order)
        };
    }

    private static (string Subject, string BodyHtml) BuildPending(Refund refund, Order order)
    {
        var subject = $"[CloseExp] Yêu cầu hoàn tiền đang chờ duyệt — Đơn {order.OrderCode}";
        var body = BuildCommonShell(refund, order, introPending, RefundNotificationKind.Pending);
        return (subject, body);
    }

    private static (string Subject, string BodyHtml) BuildApproved(Refund refund, Order order)
    {
        var subject = $"[CloseExp] Yêu cầu hoàn tiền đã được duyệt — Đơn {order.OrderCode}";
        var body = BuildCommonShell(refund, order, introApproved, RefundNotificationKind.Approved);
        return (subject, body);
    }

    private static (string Subject, string BodyHtml) BuildRejected(Refund refund, Order order)
    {
        var subject = $"[CloseExp] Yêu cầu hoàn tiền không được duyệt — Đơn {order.OrderCode}";
        var body = BuildCommonShell(refund, order, introRejected, RefundNotificationKind.Rejected);
        return (subject, body);
    }

    private static (string Subject, string BodyHtml) BuildCompleted(Refund refund, Order order)
    {
        var subject = $"[CloseExp] Hoàn tiền đã hoàn tất — Đơn {order.OrderCode}";
        var body = BuildCommonShell(refund, order, introCompleted, RefundNotificationKind.Completed);
        return (subject, body);
    }

    private static string introPending =>
        "<p>Chúng tôi đã <strong>ghi nhận một yêu cầu hoàn tiền</strong> cho đơn hàng của bạn với trạng thái <strong>đang chờ quản trị viên duyệt</strong>. " +
        "Đây <strong>không phải</strong> xác nhận tiền đã được hoàn vào tài khoản của bạn.</p>";

    private static string introApproved =>
        "<p>Yêu cầu hoàn tiền của bạn đã được <strong>phê duyệt</strong> bởi quản trị viên. " +
        "Việc chuyển khoản thực tế có thể cần thêm thời gian tùy ngân hàng / cổng thanh toán.</p>";

    private static string introRejected =>
        "<p>Yêu cầu hoàn tiền của bạn đã bị <strong>từ chối</strong>. Số tiền sẽ không được hoàn theo yêu cầu này. " +
        "Nếu cần làm rõ, vui lòng liên hệ hỗ trợ kèm mã yêu cầu hoàn bên dưới.</p>";

    private static string introCompleted =>
        "<p>Khoản hoàn cho yêu cầu này đã được <strong>hoàn tất xử lý</strong> (gồm các bước nội bộ / đối soát). " +
        "Nếu bạn chưa thấy giao dịch trên tài khoản, vui lòng kiểm tra lại sau hoặc liên hệ ngân hàng.</p>";

    private static string BuildCommonShell(Refund refund, Order order, string introHtml, RefundNotificationKind kind)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var refundedIds = ParseRefundItemIds(refund.RefundedOrderItemIdsJson);

        var sb = new StringBuilder();
        sb.Append("<html><body>");
        sb.Append("<p>Xin chào ").Append(HtmlEncode(order.User?.FullName)).Append(",</p>");
        sb.Append(introHtml);

        sb.Append("<ul>");
        sb.Append("<li><strong>Mã đơn hàng:</strong> ").Append(HtmlEncode(order.OrderCode)).Append("</li>");
        sb.Append("<li><strong>Mã yêu cầu hoàn:</strong> ").Append(refund.RefundId).Append("</li>");
        sb.Append("<li><strong>Số tiền:</strong> ").Append(refund.Amount.ToString("N0", vi)).Append(" đ</li>");
        sb.Append("<li><strong>Lý do (ghi nhận):</strong> ").Append(HtmlEncode(refund.Reason)).Append("</li>");
        if (kind != RefundNotificationKind.Pending && (refund.ProcessedAt.HasValue || !string.IsNullOrEmpty(refund.ProcessedBy)))
        {
            sb.Append("<li><strong>Xử lý:</strong> ").Append(HtmlEncode(refund.ProcessedBy ?? "—"));
            if (refund.ProcessedAt.HasValue)
                sb.Append(" — ").Append(refund.ProcessedAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append(" UTC");
            sb.Append("</li>");
        }
        sb.Append("</ul>");

        if (refundedIds is { Count: > 0 })
        {
            sb.Append("<p><strong>Các dòng hàng liên quan:</strong></p>");
            sb.Append("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;font-size:14px\">");
            sb.Append("<thead><tr><th>Sản phẩm</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>");

            var idSet = refundedIds.ToHashSet();
            foreach (var item in order.OrderItems.Where(oi => idSet.Contains(oi.OrderItemId)))
            {
                var name = item.StockLot?.Product?.Name ?? "(Sản phẩm)";
                sb.Append("<tr>");
                sb.Append("<td>").Append(HtmlEncode(name)).Append("</td>");
                sb.Append("<td style=\"text-align:right\">").Append(item.Quantity).Append("</td>");
                sb.Append("<td style=\"text-align:right\">").Append(item.UnitPrice.ToString("N0", vi)).Append(" đ</td>");
                sb.Append("<td style=\"text-align:right\">").Append(item.TotalPrice.ToString("N0", vi)).Append(" đ</td>");
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
        }
        else
        {
            sb.Append("<p><em>Khoản hoàn không gắn cụ thể với từng dòng hàng trong hệ thống.</em></p>");
        }

        sb.Append("<p>Trân trọng,<br/>CloseExp</p>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string HtmlEncode(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

    private static IReadOnlyList<Guid>? ParseRefundItemIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var list = JsonSerializer.Deserialize<List<Guid>>(json);
            return list is { Count: > 0 } ? list : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
