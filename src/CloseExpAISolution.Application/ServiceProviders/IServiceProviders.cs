using CloseExpAISolution.Application.Services.Interface;

namespace CloseExpAISolution.Application.ServiceProviders;

public interface IServiceProviders
{
    IProductService ProductService { get; }
    IMarketStaffService MarketStaffService { get; }
    ISupermarketService SupermarketService { get; }
    IProductImageService ProductImageService { get; }
    IAIVerificationLogService AIVerificationLogService { get; }
}