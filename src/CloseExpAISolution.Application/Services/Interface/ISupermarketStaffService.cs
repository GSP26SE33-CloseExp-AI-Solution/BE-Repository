using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ISupermarketStaffService
{
    Task<SupermarketStaff?> GetByIdAsync(int id);
    Task<IEnumerable<SupermarketStaff>> GetAllAsync();
    Task<IEnumerable<SupermarketStaff>> FindAsync(Expression<Func<SupermarketStaff, bool>> predicate);
    Task<SupermarketStaff?> FirstOrDefaultAsync(Expression<Func<SupermarketStaff, bool>> predicate);
    Task<SupermarketStaff> AddAsync(SupermarketStaff entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<SupermarketStaff>> AddRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupermarketStaff entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(SupermarketStaff entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<SupermarketStaff> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<SupermarketStaff, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<SupermarketStaff, bool>> predicate);

    Task<MarketStaffResponseDto?> GetByIdWithDtoAsync(Guid id);
    Task<IEnumerable<MarketStaffResponseDto>> GetAllWithDtoAsync();
    Task<MarketStaffResponseDto> CreateMarketStaffAsync(CreateMarketStaffRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateMarketStaffAsync(Guid id, UpdateMarketStaffRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteMarketStaffAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetSupermarketIdByUserIdAsync(Guid userId);
}

