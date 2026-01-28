using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class SupermarketService : BaseCrudService<Supermarket>, ISupermarketService
{
    public SupermarketService(IUnitOfWork unitOfWork)
        : base(unitOfWork, unitOfWork.SupermarketRepository)
    {
    }
}

