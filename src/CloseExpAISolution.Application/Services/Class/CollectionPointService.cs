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
        var orderCountByCollection = await GetOrderCollectionCountsAsync(cancellationToken);
        return points
            .OrderBy(x => x.Name)
            .Select(x => new CollectionPointResponseDto
            {
                CollectionId = x.CollectionId,
                Name = x.Name,
                AddressLine = x.AddressLine,
                RelatedOrderCount = orderCountByCollection.TryGetValue(x.CollectionId, out var c) ? c : 0
            });
    }

    public async Task<CollectionPointResponseDto?> GetByIdAsync(Guid collectionId, CancellationToken cancellationToken = default)
    {
        var point = await _unitOfWork.Repository<CollectionPoint>()
            .FirstOrDefaultAsync(x => x.CollectionId == collectionId);

        if (point == null)
            return null;

        var relatedOrders = await _unitOfWork.Repository<Order>().CountAsync(o => o.CollectionId == collectionId);

        return new CollectionPointResponseDto
        {
            CollectionId = point.CollectionId,
            Name = point.Name,
            AddressLine = point.AddressLine,
            RelatedOrderCount = relatedOrders
        };
    }

    private async Task<Dictionary<Guid, int>> GetOrderCollectionCountsAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _unitOfWork.Repository<Order>().GetAllAsync();
        return orders
            .Where(o => o.CollectionId.HasValue)
            .GroupBy(o => o.CollectionId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
