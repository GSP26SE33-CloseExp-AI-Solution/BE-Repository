using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;

namespace CloseExpAISolution.Infrastructure.Repositories.Interface;

public interface IBarcodeProductRepository : IGenericRepository<BarcodeProduct>
{
    Task<BarcodeProduct?> GetByBarcodeAsync(string barcode);
    Task<bool> ExistsByBarcodeAsync(string barcode);
    Task<IEnumerable<BarcodeProduct>> GetByCountryAsync(string country);
    Task<IEnumerable<BarcodeProduct>> GetVietnameseProductsAsync();
    Task<IEnumerable<BarcodeProduct>> GetPendingReviewAsync();
    Task<IEnumerable<BarcodeProduct>> SearchAsync(string searchTerm, int limit = 20);
    Task IncrementScanCountAsync(string barcode);
}
