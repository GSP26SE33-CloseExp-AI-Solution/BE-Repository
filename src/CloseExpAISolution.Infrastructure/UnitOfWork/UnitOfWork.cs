using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories;
using CloseExpAISolution.Infrastructure.Repositories.Class;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloseExpAISolution.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    //Khai báo Interface ở đây!
    private IProductRepository? _productRepository;
    private ISupermarketStaffRepository? _supermarketStaffRepository;
    private ISupermarketRepository? _supermarketRepository;
    private IProductImageRepository? _productImageRepository;
    private IAIVerificationLogRepository? _aIVerificationLogRepository;
    private IBarcodeProductRepository? _barcodeProductRepository;
    private IMarketPriceRepository? _marketPriceRepository;
    private IPriceFeedbackRepository? _priceFeedbackRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }
    //Đăng ký sử dụng repo ở đây!
    public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);
    public ISupermarketStaffRepository SupermarketStaffRepository => _supermarketStaffRepository ??= new SupermarketStaffRepository(_context);
    public ISupermarketRepository SupermarketRepository => _supermarketRepository ??= new SupermarketRepository(_context);
    public IProductImageRepository ProductImageRepository => _productImageRepository ??= new ProductImageRepository(_context);
    public IAIVerificationLogRepository AIVerificationLogRepository => _aIVerificationLogRepository ??= new AIVerificationLogRepository(_context);
    public IBarcodeProductRepository BarcodeProductRepository => _barcodeProductRepository ??= new BarcodeProductRepository(_context);
    public IMarketPriceRepository MarketPriceRepository => _marketPriceRepository ??= new MarketPriceRepository(_context);
    public IPriceFeedbackRepository PriceFeedbackRepository => _priceFeedbackRepository ??= new PriceFeedbackRepository(_context);




    //B?n d??i ??ng ??ng v?o
    public IGenericRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);

        if (!_repositories.ContainsKey(type))
        {
            var repositoryInstance = new GenericRepository<T>(_context);
            _repositories.Add(type, repositoryInstance);
        }

        return (IGenericRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();

            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        await _context.DisposeAsync();
    }
}
