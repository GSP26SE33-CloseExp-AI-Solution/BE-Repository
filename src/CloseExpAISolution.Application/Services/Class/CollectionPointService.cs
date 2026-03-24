using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Services.Class;

public class CollectionPointService : ICollectionPointService
{
    private readonly IUnitOfWork _unitOfWork;

    public CollectionPointService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CollectionPointResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var points = await _unitOfWork.Repository<CollectionPoint>().GetAllAsync();
        return points
            .OrderBy(x => x.Name)
            .Select(x => new CollectionPointResponseDto
            {
                CollectionId = x.CollectionId,
                Name = x.Name,
                AddressLine = x.AddressLine
            });
    }

    public async Task<CollectionPointResponseDto?> GetByIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var point = await _unitOfWork.Repository<CollectionPoint>()
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId);

        if (point == null)
            return null;

        return new CollectionPointResponseDto
        {
            CollectionId = point.CollectionId,
            Name = point.Name,
            AddressLine = point.AddressLine
        };
    }
}
