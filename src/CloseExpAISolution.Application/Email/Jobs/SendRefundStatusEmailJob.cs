using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class SendRefundStatusEmailJob : IJob
{
    private const string RefundIdKey = "refundId";
    private const string EventNameKey = "eventName";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendRefundStatusEmailJob> _logger;

    public SendRefundStatusEmailJob(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SendRefundStatusEmailJob> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var refundIdRaw = context.JobDetail.JobDataMap.GetString(RefundIdKey);
        if (!Guid.TryParse(refundIdRaw, out var refundId))
        {
            _logger.LogWarning("SendRefundStatusEmailJob: invalid refundId={RefundIdRaw}", refundIdRaw);
            return;
        }

        var eventName = context.JobDetail.JobDataMap.GetString(EventNameKey) ?? "updated";
        var refund = await _unitOfWork.Repository<Refund>().FirstOrDefaultAsync(r => r.RefundId == refundId);
        if (refund == null)
            return;

        var order = await _unitOfWork.Repository<Order>().FirstOrDefaultAsync(o => o.OrderId == refund.OrderId);
        if (order == null)
            return;

        var user = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == order.UserId);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return;

        var (subject, body) = BuildEmailContent(order, refund, user, eventName);

        await _emailService.SendEmailAsync(user.Email, subject, body);
    }

    private static (string Subject, string Body) BuildEmailContent(
        Order order,
        Refund refund,
        User user,
        string eventName)
    {
        var orderCode = order.OrderCode ?? order.OrderId.ToString();
        var customerName = string.IsNullOrWhiteSpace(user.FullName) ? "Quý khách" : user.FullName;
        var amountText = string.Format("{0:N0} VND", refund.Amount);
        var reasonText = string.IsNullOrWhiteSpace(refund.Reason) ? "Không có" : refund.Reason;
        var statusVi = ToVietnameseStatus(refund.Status);

        var (title, message) = eventName.ToLowerInvariant() switch
        {
            "created" => (
                "Yêu cầu hoàn tiền đã được ghi nhận",
                "Chúng tôi đã tiếp nhận yêu cầu hoàn tiền của bạn và sẽ xử lý trong thời gian sớm nhất."
            ),
            "approved" => (
                "Yêu cầu hoàn tiền đã được phê duyệt",
                "Yêu cầu hoàn tiền của bạn đã được phê duyệt và đang chờ hoàn tất."
            ),
            "rejected" => (
                "Yêu cầu hoàn tiền bị từ chối",
                "Yêu cầu hoàn tiền của bạn hiện chưa được chấp nhận. Vui lòng kiểm tra lý do bên dưới."
            ),
            "completed" => (
                "Hoàn tiền đã hoàn tất",
                "Khoản hoàn tiền của bạn đã được xử lý thành công."
            ),
            _ => (
                "Cập nhật yêu cầu hoàn tiền",
                "Yêu cầu hoàn tiền của bạn vừa được cập nhật trạng thái."
            )
        };

        var subject = $"[CloseExp] {title} - {orderCode}";
        var body = $@"
<html><body style='font-family: Arial, sans-serif;'>
<p>Xin chào {customerName},</p>
<p>{message}</p>
<ul>
  <li>Đơn hàng: <b>{orderCode}</b></li>
  <li>Trạng thái hoàn tiền: <b>{statusVi}</b></li>
  <li>Số tiền: <b>{amountText}</b></li>
  <li>Lý do: {reasonText}</li>
</ul>
<p style='color:#666;font-size:12px'>Nếu bạn cần hỗ trợ thêm, vui lòng liên hệ bộ phận CSKH của CloseExp.</p>
</body></html>";

        return (subject, body);
    }

    private static string ToVietnameseStatus(RefundState status) => status switch
    {
        RefundState.Pending => "Đang chờ xử lý",
        RefundState.Approved => "Đã phê duyệt",
        RefundState.Rejected => "Đã từ chối",
        RefundState.Completed => "Đã hoàn tất",
        _ => status.ToString()
    };
}

