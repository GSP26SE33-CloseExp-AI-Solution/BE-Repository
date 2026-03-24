using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryResponseDto> CreateAsync(CreateCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, UpdateCategoryRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
