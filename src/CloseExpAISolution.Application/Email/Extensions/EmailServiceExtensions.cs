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

                var marketJobKey = new JobKey("MarketPriceRefreshJob");
                q.AddJob<MarketPriceRefreshJob>(opts => opts.WithIdentity(marketJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(marketJobKey)
                    .WithIdentity("MarketPriceRefreshJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(30)
                        .RepeatForever())
                );

                var deliveryQrJobKey = new JobKey("SendOrderDeliveryQrEmailJob");
                q.AddJob<SendOrderDeliveryQrEmailJob>(opts => opts.WithIdentity(deliveryQrJobKey).StoreDurably());

                var refundOutboxJobKey = new JobKey("ProcessRefundEmailOutboxJob");
                q.AddJob<ProcessRefundEmailOutboxJob>(opts => opts.WithIdentity(refundOutboxJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(refundOutboxJobKey)
                    .WithIdentity("ProcessRefundEmailOutboxJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(15)
                        .RepeatForever()));

                var autoConfirmDeliveredOrdersJobKey = new JobKey("AutoConfirmDeliveredOrdersJob");
                q.AddJob<AutoConfirmDeliveredOrdersJob>(opts => opts.WithIdentity(autoConfirmDeliveredOrdersJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(autoConfirmDeliveredOrdersJobKey)
                    .WithIdentity("AutoConfirmDeliveredOrdersJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(15)
                        .RepeatForever()));

                var autoRefundStaleRtsJobKey = new JobKey("AutoRefundStaleReadyToShipOrdersJob");
                q.AddJob<AutoRefundStaleReadyToShipOrdersJob>(opts => opts.WithIdentity(autoRefundStaleRtsJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(autoRefundStaleRtsJobKey)
                    .WithIdentity("AutoRefundStaleReadyToShipOrdersJob-trigger")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(5)
                        .RepeatForever()));

                var cancelPendingOrdersByTodayExpiryJobKey = new JobKey("CancelPendingOrdersByTodayExpiryJob");
                q.AddJob<CancelPendingOrdersByTodayExpiryJob>(opts => opts.WithIdentity(cancelPendingOrdersByTodayExpiryJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(cancelPendingOrdersByTodayExpiryJobKey)
                    .WithIdentity("CancelPendingOrdersByTodayExpiryJob-trigger")
                    .WithCronSchedule(
                        "0 0 21 * * ?",
                        x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"))));
            });
            services.AddQuartzHostedService(options =>
            {
                options.WaitForJobsToComplete = true;
            });

            return services;
        }
    }
}