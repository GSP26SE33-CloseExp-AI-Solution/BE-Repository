using AutoMapper;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Clients;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using CloseExpAISolution.Application.Email.Clients;
using CloseExpAISolution.Application.Email.Interfaces;

namespace CloseExpAISolution.Application.ServiceProviders
{
        public class ServiceProviders : IServiceProviders
        {
                private readonly IUnitOfWork _unitOfWork;
                private readonly IHttpContextAccessor _httpContextAccessor;
                private readonly ApplicationDbContext _context;
                private readonly IConfiguration _configuration;
                private readonly IMapper _mapper;
                private readonly IServiceProvider _serviceProvider;

                private IProductService? _productService;
                private ISupermarketStaffService? _marketStaffService;
                private ISupermarketService? _supermarketService;
                private IProductImageService? _productImageService;
                private IAIVerificationLogService? _aIVerificationLogService;
                private IAuthService? _authService;
                private IUserService? _userService;
                private IR2StorageService? _r2StorageService;
                private IFeedbackService? _feedbackService;
                private IUserImageService? _userImageService;
                private IBarcodeLookupService? _barcodeLookupService;
                private IAIProductService? _aIProductService;
                private IMarketPriceService? _marketPriceService;
                private IProductWorkflowService? _productWorkflowService;
                private IExcelImportService? _excelImportService;
                private IDeliveryService? _deliveryService;
                private IPackagingService? _packagingService;
                private IEmailService? _emailService;
                private IMapboxService? _mapboxService;

                public ServiceProviders(
                    IUnitOfWork unitOfWork,
                    IHttpContextAccessor httpContextAccessor,
                    ApplicationDbContext context,
                    IConfiguration configuration,
                    IMapper mapper,
                    IServiceProvider serviceProvider)
                {
                        _unitOfWork = unitOfWork;
                        _httpContextAccessor = httpContextAccessor;
                        _configuration = configuration;
                        _context = context;
                        _mapper = mapper;
                        _serviceProvider = serviceProvider;
                }
                public IProductService ProductService => _productService ??= new ProductService(_unitOfWork, _context, _mapper);
                public ISupermarketStaffService MarketStaffService => _marketStaffService ??= new SupermarketStaffService(_unitOfWork, _mapper);
                public ISupermarketService SupermarketService => _supermarketService ??= new SupermarketService(_unitOfWork, _mapper);
                public IProductImageService ProductImageService => _productImageService ??= new ProductImageService(_unitOfWork);
                public IAIVerificationLogService AIVerificationLogService => _aIVerificationLogService ??= new AIVerificationLogService(_unitOfWork);
                public IAuthService AuthService => _authService ??= ActivatorUtilities.CreateInstance<AuthService>(_serviceProvider);
                public IUserService UserService => _userService ??= ActivatorUtilities.CreateInstance<UserService>(_serviceProvider);
                public IR2StorageService R2StorageService => _r2StorageService ??= new R2StorageService(_unitOfWork, _configuration);
                public IFeedbackService FeedbackService => _feedbackService ??= new FeedbackService(_unitOfWork, _mapper);
                public IUserImageService UserImageService => _userImageService ??= new UserImageService(_unitOfWork, R2StorageService);
                public IBarcodeLookupService BarcodeLookupService => _barcodeLookupService ??= ActivatorUtilities.CreateInstance<BarcodeLookupService>(_serviceProvider);
                public IAIProductService AIProductService => _aIProductService ??= ActivatorUtilities.CreateInstance<AIProductService>(_serviceProvider);
                public IMarketPriceService MarketPriceService => _marketPriceService ??= ActivatorUtilities.CreateInstance<MarketPriceService>(_serviceProvider);
                public IProductWorkflowService ProductWorkflowService => _productWorkflowService ??= ActivatorUtilities.CreateInstance<ProductWorkflowService>(_serviceProvider);
                public IExcelImportService ExcelImportService => _excelImportService ??= ActivatorUtilities.CreateInstance<ExcelImportService>(_serviceProvider);
                public IDeliveryService DeliveryService => _deliveryService ??= ActivatorUtilities.CreateInstance<DeliveryService>(_serviceProvider);
                public IPackagingService PackagingService => _packagingService ??= ActivatorUtilities.CreateInstance<PackagingService>(_serviceProvider);
                public IEmailService EmailService => _emailService ??= ActivatorUtilities.CreateInstance<EmailService>(_serviceProvider);
                public IMapboxService MapboxService => _mapboxService ??= _serviceProvider.GetRequiredService<IMapboxService>();
        }
}

