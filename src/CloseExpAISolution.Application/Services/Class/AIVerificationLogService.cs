using System.Linq.Expressions;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class AIVerificationLogService : IAIVerificationLogService
{
    private readonly IUnitOfWork _unitOfWork;

    public AIVerificationLogService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<AIVerificationLog?> GetByIdAsync(int id) => _unitOfWork.AIVerificationLogRepository.GetByIdAsync(id);
    public Task<IEnumerable<AIVerificationLog>> GetAllAsync() => _unitOfWork.AIVerificationLogRepository.GetAllAsync();
    public Task<IEnumerable<AIVerificationLog>> FindAsync(Expression<Func<AIVerificationLog, bool>> predicate) => _unitOfWork.AIVerificationLogRepository.FindAsync(predicate);
    public Task<AIVerificationLog?> FirstOrDefaultAsync(Expression<Func<AIVerificationLog, bool>> predicate) => _unitOfWork.AIVerificationLogRepository.FirstOrDefaultAsync(predicate);
    public Task<int> CountAsync(Expression<Func<AIVerificationLog, bool>>? predicate = null) => _unitOfWork.AIVerificationLogRepository.CountAsync(predicate);
    public Task<bool> ExistsAsync(Expression<Func<AIVerificationLog, bool>> predicate) => _unitOfWork.AIVerificationLogRepository.ExistsAsync(predicate);

    public async Task<AIVerificationLog> AddAsync(AIVerificationLog entity, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.AIVerificationLogRepository.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task<IEnumerable<AIVerificationLog>> AddRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default)
    {
        var added = await _unitOfWork.AIVerificationLogRepository.AddRangeAsync(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return added;
    }

    public async Task UpdateAsync(AIVerificationLog entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.AIVerificationLogRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.AIVerificationLogRepository.UpdateRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AIVerificationLog entity, CancellationToken cancellationToken = default)
    {
        _unitOfWork.AIVerificationLogRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default)
    {
        _unitOfWork.AIVerificationLogRepository.DeleteRange(entities);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

