using System.Linq.Expressions;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IAIVerificationLogService
{
    Task<AIVerificationLog?> GetByIdAsync(int id);
    Task<IEnumerable<AIVerificationLog>> GetAllAsync();
    Task<IEnumerable<AIVerificationLog>> FindAsync(Expression<Func<AIVerificationLog, bool>> predicate);
    Task<AIVerificationLog?> FirstOrDefaultAsync(Expression<Func<AIVerificationLog, bool>> predicate);
    Task<AIVerificationLog> AddAsync(AIVerificationLog entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<AIVerificationLog>> AddRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(AIVerificationLog entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(AIVerificationLog entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<AIVerificationLog> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<AIVerificationLog, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<AIVerificationLog, bool>> predicate);
}

