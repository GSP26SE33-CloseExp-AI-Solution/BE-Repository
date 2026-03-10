using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetByIdWithWorkflowDetailsAsync(Guid productId)
    {
        return await _context.Products
            .Include(p => p.ProductDetail)
            .Include(p => p.ProductLots)
            .FirstOrDefaultAsync(p => p.ProductId == productId);
    }
}
