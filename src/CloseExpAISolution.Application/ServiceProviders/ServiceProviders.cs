using AutoMapper;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CloseExpAISolution.Application.ServiceProviders
{
    public class ServiceProviders : IServiceProviders
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        private IProductService? _productService;
        private IMarketStaffService? _marketStaffService;
        private ISupermarketService? _supermarketService;
        private IProductImageService? _productImageService;
        private IAIVerificationLogService? _aIVerificationLogService;
        private IAuthService? _authService;
        private IUserService? _userService;
        private IR2StorageService? _r2StorageService;
        private IFeedbackService? _feedbackService;

        public ServiceProviders(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ApplicationDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _context = context;
            _mapper = mapper;
        }
        public IProductService ProductService => _productService ??= new ProductService(_unitOfWork, _context, _mapper);
        public IMarketStaffService MarketStaffService => _marketStaffService ??= new MarketStaffService(_unitOfWork, _mapper);
        public ISupermarketService SupermarketService => _supermarketService ??= new SupermarketService(_unitOfWork, _mapper);
        public IProductImageService ProductImageService => _productImageService ??= new ProductImageService(_unitOfWork);
        public IAIVerificationLogService AIVerificationLogService => _aIVerificationLogService ??= new AIVerificationLogService(_unitOfWork);
        public IAuthService AuthService => _authService ??= new AuthService(_unitOfWork, _configuration);
        public IUserService UserService => _userService ??= new UserService(_unitOfWork, _mapper);
        public IR2StorageService R2StorageService => _r2StorageService ??= new R2StorageService(_unitOfWork, _configuration);
        public IFeedbackService FeedbackService => _feedbackService ??= new FeedbackService(_unitOfWork, _mapper);
    }
}
