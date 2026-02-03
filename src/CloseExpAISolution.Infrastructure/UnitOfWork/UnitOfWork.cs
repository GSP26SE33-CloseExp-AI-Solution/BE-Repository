using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Class;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore.Storage;

namespace CloseExpAISolution.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    //Khai b?o Interface ? ??y!
    private IProductRepository? _productRepository;
    private IMarketStaffRepository? _marketStaffRepository;
    private ISupermarketRepository? _supermarketRepository;
    private IProductImageRepository? _productImageRepository;
    private IAIVerificationLogRepository? _aIVerificationLogRepository;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }
    //??ng k? s? d?ng repo ? ??y!
    public IProductRepository ProductRepository => _productRepository ??= new ProductRepository(_context);
    public IMarketStaffRepository MarketStaffRepository => _marketStaffRepository ??= new MarketStaffRepository(_context);
    public ISupermarketRepository SupermarketRepository => _supermarketRepository ??= new SupermarketRepository(_context);
    public IProductImageRepository ProductImageRepository => _productImageRepository ??= new ProductImageRepository(_context);
    public IAIVerificationLogRepository AIVerificationLogRepository => _aIVerificationLogRepository ??= new AIVerificationLogRepository(_context);




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
