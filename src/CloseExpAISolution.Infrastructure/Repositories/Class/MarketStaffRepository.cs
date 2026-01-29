using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class MarketStaffRepository : GenericRepository<MarketStaff>, IMarketStaffRepository
{
    public MarketStaffRepository(ApplicationDbContext context) : base(context)
    {
    }
}
