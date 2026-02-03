using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Repositories;
using CloseExpAISolution.Infrastructure.Repositories.Interface;

namespace CloseExpAISolution.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{

    IProductRepository ProductRepository { get; }
    IMarketStaffRepository MarketStaffRepository { get; }
    ISupermarketRepository SupermarketRepository { get; }
    IProductImageRepository ProductImageRepository { get; }
    IAIVerificationLogRepository AIVerificationLogRepository { get; }
    IBarcodeProductRepository BarcodeProductRepository { get; }
    IMarketPriceRepository MarketPriceRepository { get; }
    IPriceFeedbackRepository PriceFeedbackRepository { get; }

    //Bên dưới đừng dụng vào
    IGenericRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
