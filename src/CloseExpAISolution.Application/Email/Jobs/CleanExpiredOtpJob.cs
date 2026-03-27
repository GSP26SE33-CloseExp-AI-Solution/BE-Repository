using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CloseExpAISolution.Application.Email.Jobs;

[DisallowConcurrentExecution]
public class CleanExpiredOtpJob : IJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleanExpiredOtpJob> _logger;

    public CleanExpiredOtpJob(IUnitOfWork unitOfWork, ILogger<CleanExpiredOtpJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("CleanExpiredOtpJob: Bắt đầu dọn dẹp OTP hết hạn...");

            var userRepo = _unitOfWork.Repository<User>();
            var now = DateTime.UtcNow;

            var usersWithExpiredOtp = await userRepo.FindAsync(
                u => u.OtpCode != null && u.OtpExpiresAt != null && u.OtpExpiresAt < now
            );

            if (!usersWithExpiredOtp.Any())
            {
                _logger.LogInformation("CleanExpiredOtpJob: Không có OTP hết hạn cần dọn dẹp.");
                return;
            }

            var count = 0;
            foreach (var user in usersWithExpiredOtp)
            {
                user.OtpCode = null;
                user.OtpExpiresAt = null;
                user.OtpFailedCount = 0;
                userRepo.Update(user);
                count++;
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("CleanExpiredOtpJob: Đã dọn dẹp {Count} OTP hết hạn.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CleanExpiredOtpJob: Lỗi khi dọn dẹp OTP hết hạn.");
        }
    }
}
