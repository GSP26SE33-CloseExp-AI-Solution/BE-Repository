using CloseExpAISolution.Application.Email.Clients;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Email.Jobs;
using CloseExpAISolution.Application.Email.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace CloseExpAISolution.Application.Email.Extensions
{
    public static class EmailServiceExtensions
    {
        public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind EmailSettings từ configuration
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                ?? throw new InvalidOperationException("EmailSettings configuration is missing");

            // Đăng ký EmailSettings như singleton
            services.AddSingleton(emailSettings);

            // Đăng ký IOptions<EmailSettings> nếu cần
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

            // Đăng ký EmailService
            services.AddTransient<IEmailService, EmailService>();

            // Cấu hình Quartz cho background jobs
            services.AddQuartz(q =>
            {
                // Đăng ký CleanExpiredOtpJob - dọn dẹp OTP hết hạn mỗi 30 phút
                var jobKey = new JobKey("CleanExpiredOtpJob");
                q.AddJob<CleanExpiredOtpJob>(opts => opts.WithIdentity(jobKey));
                q.AddTrigger(opts => opts
                    .ForJob(jobKey)
                    .WithIdentity("CleanExpiredOtpJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(30)
                        .RepeatForever())
                );
            });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}