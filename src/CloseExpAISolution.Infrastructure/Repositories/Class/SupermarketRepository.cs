using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class SupermarketRepository : GenericRepository<Supermarket>, ISupermarketRepository
{
    public SupermarketRepository(ApplicationDbContext context) : base(context)
    {
    }
}
