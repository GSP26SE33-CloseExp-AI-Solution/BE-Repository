using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICollectionPointService
{
    Task<IEnumerable<CollectionPointResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CollectionPointResponseDto?> GetByIdAsync(Guid collectionId, CancellationToken cancellationToken = default);
}
