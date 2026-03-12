using System.Linq.Expressions;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ISupermarketService
{
    Task<Supermarket?> GetByIdAsync(int id);
    Task<IEnumerable<Supermarket>> GetAllAsync();
    Task<IEnumerable<Supermarket>> FindAsync(Expression<Func<Supermarket, bool>> predicate);
    Task<Supermarket?> FirstOrDefaultAsync(Expression<Func<Supermarket, bool>> predicate);
    Task<Supermarket> AddAsync(Supermarket entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<Supermarket>> AddRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(Supermarket entity, CancellationToken cancellationToken = default);
    Task UpdateRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default);
    Task DeleteAsync(Supermarket entity, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<Supermarket> entities, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<Supermarket, bool>>? predicate = null);
    Task<bool> ExistsAsync(Expression<Func<Supermarket, bool>> predicate);

    Task<SupermarketResponseDto?> GetByIdWithDtoAsync(Guid id);
    Task<IEnumerable<SupermarketResponseDto>> GetAllWithDtoAsync();
    Task<IEnumerable<SupermarketResponseDto>> GetAvailableWithDtoAsync();
    Task<IEnumerable<SupermarketResponseDto>> SearchAsync(string query);
    Task<SupermarketResponseDto> CreateSupermarketAsync(CreateSupermarketRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateSupermarketAsync(Guid id, UpdateSupermarketRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteSupermarketAsync(Guid id, CancellationToken cancellationToken = default);
}