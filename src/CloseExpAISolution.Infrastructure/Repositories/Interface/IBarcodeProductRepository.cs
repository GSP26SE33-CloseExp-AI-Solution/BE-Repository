using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;

namespace CloseExpAISolution.Infrastructure.Repositories.Interface;

/// <summary>
/// Repository interface cho BarcodeProduct entity.
/// Hỗ trợ cơ chế Cache & Crowd-source cho barcode lookup.
/// </summary>
public interface IBarcodeProductRepository : IGenericRepository<BarcodeProduct>
{
    /// <summary>
    /// Tìm sản phẩm theo barcode
    /// </summary>
    Task<BarcodeProduct?> GetByBarcodeAsync(string barcode);
    
    /// <summary>
    /// Kiểm tra barcode đã tồn tại chưa
    /// </summary>
    Task<bool> ExistsByBarcodeAsync(string barcode);
    
    /// <summary>
    /// Lấy danh sách sản phẩm theo quốc gia
    /// </summary>
    Task<IEnumerable<BarcodeProduct>> GetByCountryAsync(string country);
    
    /// <summary>
    /// Lấy danh sách sản phẩm Việt Nam
    /// </summary>
    Task<IEnumerable<BarcodeProduct>> GetVietnameseProductsAsync();
    
    /// <summary>
    /// Lấy danh sách sản phẩm cần review (từ manual/ai-ocr)
    /// </summary>
    Task<IEnumerable<BarcodeProduct>> GetPendingReviewAsync();
    
    /// <summary>
    /// Tìm kiếm sản phẩm theo tên hoặc brand
    /// </summary>
    Task<IEnumerable<BarcodeProduct>> SearchAsync(string searchTerm, int limit = 20);
    
    /// <summary>
    /// Tăng số lần quét
    /// </summary>
    Task IncrementScanCountAsync(string barcode);
}
