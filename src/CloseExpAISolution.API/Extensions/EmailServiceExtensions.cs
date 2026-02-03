using CloseExpAISolution.Application.Email.Settings;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using Quartz;

namespace CloseExpAISolution.API.Extensions
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
            services.AddQuartz(q => { });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}