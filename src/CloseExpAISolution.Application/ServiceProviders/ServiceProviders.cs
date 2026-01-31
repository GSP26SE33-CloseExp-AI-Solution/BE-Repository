using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Http;

namespace CloseExpAISolution.Application.ServiceProviders
{
    public class ServiceProviders : IServiceProviders
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        private IProductService _productService;
        private IMarketStaffService _marketStaffService;
        private ISupermarketService _supermarketService;
        private IProductImageService _productImageService;
        private IAIVerificationLogService _aIVerificationLogService;

        public ServiceProviders(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }
        public IProductService ProductService => _productService ??= new ProductService(_unitOfWork, _context);
        public IMarketStaffService MarketStaffService => _marketStaffService ??= new MarketStaffService(_unitOfWork);
        public ISupermarketService SupermarketService => _supermarketService ??= new SupermarketService(_unitOfWork);
        public IProductImageService ProductImageService => _productImageService ??= new ProductImageService(_unitOfWork);
        public IAIVerificationLogService AIVerificationLogService => _aIVerificationLogService ??= new AIVerificationLogService(_unitOfWork);
    }
}