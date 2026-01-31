using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface IMarketStaffService
{
    Task<MarketStaff?> GetByIdAsync(int id);
    Task<IEnumerable<MarketStaff>> GetAllAsync();
    Task<IEnumerable<MarketStaff>> FindAsync(Expression<Func<MarketStaff, bool>> predicate);
    Task<MarketStaff?> FirstOrDefaultAsync(Expression<Func<MarketStaff, bool>> predicate);
    Task<MarketStaff> AddAsync(MarketStaff entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketStaff>> AddRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(MarketStaff entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(MarketStaff entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<MarketStaff> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<MarketStaff, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<MarketStaff, bool>> predicate);
    
    Task<MarketStaffResponseDto?> GetByIdWithDtoAsync(Guid id);
    Task<IEnumerable<MarketStaffResponseDto>> GetAllWithDtoAsync();
    Task<MarketStaffResponseDto> CreateMarketStaffAsync(CreateMarketStaffRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateMarketStaffAsync(Guid id, UpdateMarketStaffRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteMarketStaffAsync(Guid id, CancellationToken cancellationToken = default);
}

