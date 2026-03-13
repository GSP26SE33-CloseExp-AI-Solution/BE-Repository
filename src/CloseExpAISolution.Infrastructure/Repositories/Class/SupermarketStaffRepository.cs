using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class SupermarketStaffRepository : GenericRepository<SupermarketStaff>, ISupermarketStaffRepository
{
    public SupermarketStaffRepository(ApplicationDbContext context) : base(context)
    {
    }
}
