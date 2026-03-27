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
            var emailSettings = configuration.GetSection("EmailSettings").Get<EmailSettings>()
                ?? new EmailSettings();

            services.AddSingleton(emailSettings);
            services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
            services.AddTransient<IEmailService, EmailService>();

            services.AddQuartz(q =>
            {
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