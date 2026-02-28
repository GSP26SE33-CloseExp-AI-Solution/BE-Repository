using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.Services;

namespace CloseExpAISolution.Application.ServiceProviders;

public interface IServiceProviders
{
    IProductService ProductService { get; }
    IMarketStaffService MarketStaffService { get; }
    ISupermarketService SupermarketService { get; }
    IProductImageService ProductImageService { get; }
    IAIVerificationLogService AIVerificationLogService { get; }
    IAuthService AuthService { get; }
    IUserService UserService { get; }
    IFeedbackService FeedbackService { get; }
    IR2StorageService R2StorageService { get; }
    IUserImageService UserImageService { get; }
    IEmailService EmailService { get; }
    IBarcodeLookupService BarcodeLookupService { get; }
    IAIProductService AIProductService { get; }
    IMarketPriceService MarketPriceService { get; }
    IProductWorkflowService ProductWorkflowService { get; }
    IExcelImportService ExcelImportService { get; }
    IDeliveryService DeliveryService { get; }
}