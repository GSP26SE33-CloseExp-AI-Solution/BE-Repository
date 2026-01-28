using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class ProductService : BaseCrudService<Product>, IProductService
{
    public ProductService(IUnitOfWork unitOfWork)
        : base(unitOfWork, unitOfWork.ProductRepository)
    {
    }
}

