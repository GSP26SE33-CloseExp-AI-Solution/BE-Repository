using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class BarcodeProductRepository : GenericRepository<BarcodeProduct>, IBarcodeProductRepository
{
    private new readonly ApplicationDbContext _context;

    public BarcodeProductRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<BarcodeProduct?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return null;

        var normalizedBarcode = NormalizeBarcode(barcode);
        return await _context.BarcodeProducts
            .FirstOrDefaultAsync(bp => bp.Barcode == normalizedBarcode && bp.Status == "active");
    }

    public async Task<bool> ExistsByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return false;

        var normalizedBarcode = NormalizeBarcode(barcode);
        return await _context.BarcodeProducts
            .AnyAsync(bp => bp.Barcode == normalizedBarcode);
    }

    public async Task<IEnumerable<BarcodeProduct>> GetByCountryAsync(string country)
    {
        return await _context.BarcodeProducts
            .Where(bp => bp.Country == country && bp.Status == "active")
            .OrderByDescending(bp => bp.ScanCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<BarcodeProduct>> GetVietnameseProductsAsync()
    {
        return await _context.BarcodeProducts
            .Where(bp => bp.IsVietnameseProduct && bp.Status == "active")
            .OrderByDescending(bp => bp.ScanCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<BarcodeProduct>> GetPendingReviewAsync()
    {
        return await _context.BarcodeProducts
            .Where(bp => bp.Status == "pending_review" ||
                        (bp.Source == "manual" && !bp.IsVerified) ||
                        (bp.Source == "ai-ocr" && !bp.IsVerified))
            .OrderByDescending(bp => bp.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<BarcodeProduct>> SearchAsync(string searchTerm, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<BarcodeProduct>();

        var lowerTerm = searchTerm.ToLower();
        return await _context.BarcodeProducts
            .Where(bp => bp.Status == "active" &&
                        (bp.ProductName.ToLower().Contains(lowerTerm) ||
                         (bp.Brand != null && bp.Brand.ToLower().Contains(lowerTerm)) ||
                         bp.Barcode.Contains(searchTerm)))
            .OrderByDescending(bp => bp.ScanCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task IncrementScanCountAsync(string barcode)
    {
        var normalizedBarcode = NormalizeBarcode(barcode);
        var product = await _context.BarcodeProducts
            .FirstOrDefaultAsync(bp => bp.Barcode == normalizedBarcode);

        if (product != null)
        {
            product.ScanCount++;
            _context.BarcodeProducts.Update(product);
            await _context.SaveChangesAsync();
        }
    }

    private static string NormalizeBarcode(string barcode)
    {
        return new string(barcode.Where(char.IsDigit).ToArray());
    }
}
