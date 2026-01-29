using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CloseExpAISolution.Application.ServiceProviders
{
    public class ServiceProviders : IServiceProviders
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        private IProductService _productService;
        private IMarketStaffService _marketStaffService;
        private ISupermarketService _supermarketService;
        private IProductImageService _productImageService;
        private IAIVerificationLogService _aIVerificationLogService;
        private IAuthService _authService;
        private IUserService _userService;

        public ServiceProviders(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }
        //Đăng kí các Service ở đây!
        public IProductService ProductService => _productService ??= new ProductService(_unitOfWork);
        public IMarketStaffService MarketStaffService => _marketStaffService ??= new MarketStaffService(_unitOfWork);
        public ISupermarketService SupermarketService => _supermarketService ??= new SupermarketService(_unitOfWork);
        public IProductImageService ProductImageService => _productImageService ??= new ProductImageService(_unitOfWork);
        public IAIVerificationLogService AIVerificationLogService => _aIVerificationLogService ??= new AIVerificationLogService(_unitOfWork);
        public IAuthService AuthService => _authService ??= new AuthService(_unitOfWork, _configuration);
        public IUserService UserService => _userService ??= new UserService(_unitOfWork);
    }
}
