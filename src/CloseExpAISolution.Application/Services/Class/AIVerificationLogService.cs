using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class AIVerificationLogService : BaseCrudService<AIVerificationLog>, IAIVerificationLogService
{
    public AIVerificationLogService(IUnitOfWork unitOfWork)
        : base(unitOfWork, unitOfWork.AIVerificationLogRepository)
    {
    }
}

