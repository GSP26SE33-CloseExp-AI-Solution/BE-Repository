using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductImageService : BaseCrudService<ProductImage>, IProductImageService
{
    public ProductImageService(IUnitOfWork unitOfWork)
        : base(unitOfWork, unitOfWork.ProductImageRepository)
    {
    }
}

