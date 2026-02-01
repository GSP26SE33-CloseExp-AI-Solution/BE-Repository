using CloseExpAISolution.Application.Services.Interface;

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
}