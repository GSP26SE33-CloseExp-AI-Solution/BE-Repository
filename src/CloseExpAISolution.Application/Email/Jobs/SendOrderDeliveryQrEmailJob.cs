using System.Globalization;
using System.Net.Mime;
using System.Text;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using QRCoder;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class SendOrderDeliveryQrEmailJob : IJob
{
    private const string JobDataOrderIdKey = "orderId";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendOrderDeliveryQrEmailJob> _logger;

    public SendOrderDeliveryQrEmailJob(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SendOrderDeliveryQrEmailJob> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startedAt = DateTime.UtcNow;
        try
        {
            var orderIdRaw = context.JobDetail.JobDataMap.GetString(JobDataOrderIdKey);
            if (string.IsNullOrWhiteSpace(orderIdRaw))
            {
                _logger.LogWarning("SendOrderDeliveryQrEmailJob: missing orderId job data");
                return;
            }

            if (!Guid.TryParse(orderIdRaw, out var orderId))
            {
                _logger.LogWarning("SendOrderDeliveryQrEmailJob: invalid orderId={OrderIdRaw}", orderIdRaw);
                return;
            }

            var order = await _unitOfWork.Repository<Order>()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                _logger.LogWarning("SendOrderDeliveryQrEmailJob: order not found: {OrderId}", orderId);
                return;
            }

            var user = await _unitOfWork.Repository<User>()
                .FirstOrDefaultAsync(u => u.UserId == order.UserId);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
            {
                _logger.LogWarning("SendOrderDeliveryQrEmailJob: missing email for order. orderId={OrderId}, userId={UserId}",
                    orderId, order.UserId);
                return;
            }

            var orderCode = order.OrderCode?.Trim();
            if (string.IsNullOrWhiteSpace(orderCode))
            {
                _logger.LogWarning("SendOrderDeliveryQrEmailJob: missing OrderCode. orderId={OrderId}", orderId);
                return;
            }

            var qrPngBytes = GenerateQrPngBytes(orderCode);
            var qrBase64 = Convert.ToBase64String(qrPngBytes);

            var subject = $"[CloseExp] Xác nhận giao hàng - {orderCode}";
            var body = BuildEmailBody(orderCode, qrBase64);

            await _emailService.SendEmailAsync(user.Email, subject, body, context.CancellationToken);

            _logger.LogInformation("SendOrderDeliveryQrEmailJob completed. orderId={OrderId}, to={Email}, durationMs={DurationMs}",
                orderId, user.Email, (DateTime.UtcNow - startedAt).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendOrderDeliveryQrEmailJob failed. durationMs={DurationMs}",
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
        }
    }

    private static byte[] GenerateQrPngBytes(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var qrData = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);

        var qr = new PngByteQRCode(qrData);
        return qr.GetGraphic(pixelsPerModule: 20);
    }

    private static string BuildEmailBody(string orderCode, string qrBase64)
    {
        return $@"
<!DOCTYPE html>
<html lang='vi'>
<body style='font-family: Arial, sans-serif;'>
  <p>Xin chào,</p>
  <p>Đơn hàng <b>{orderCode}</b> đã sẵn sàng giao.</p>

  <p>Mã xác nhận giao hàng (dùng để kiểm tra khi giao):</p>
  <div style='font-size: 20px; font-weight: 700; margin: 10px 0;'>{orderCode}</div>

  <p>Quét QR bên dưới để xác nhận giao hàng:</p>
  <img src='data:image/png;base64,{qrBase64}' alt='QR code' style='width: 180px; height: auto;' />

  <p style='color:#666; font-size:12px; margin-top:20px;'>
    Lưu ý: QR chỉ encode đúng mã đơn hàng (<b>{orderCode}</b>).
  </p>
</body>
</html>";
    }
}

