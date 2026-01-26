using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.Repositories.Interface;

namespace CloseExpAISolution.Infrastructure.Repositories.Class;

public class AIVerificationLogRepository : GenericRepository<AIVerificationLog>, IAIVerificationLogRepository
{
    public AIVerificationLogRepository(ApplicationDbContext context) : base(context)
    {
    }
}
