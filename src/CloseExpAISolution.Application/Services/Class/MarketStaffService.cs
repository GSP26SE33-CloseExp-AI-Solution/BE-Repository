using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class MarketStaffService : BaseCrudService<MarketStaff>, IMarketStaffService
{
    public MarketStaffService(IUnitOfWork unitOfWork)
        : base(unitOfWork, unitOfWork.MarketStaffRepository)
    {
    }
}

