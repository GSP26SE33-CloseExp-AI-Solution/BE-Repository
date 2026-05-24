using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Auth;

public static class PackagingStaffMembership
{
    public static async Task UpsertAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var repo = unitOfWork.Repository<PackagingStaff>();
        var existing = await repo.FirstOrDefaultAsync(ps => ps.UserId == userId);

        if (existing == null)
        {
            await repo.AddAsync(new PackagingStaff
            {
                PackagingStaffId = Guid.NewGuid(),
                UserId = userId,
                SupermarketId = supermarketId,
                Status = PackagingStaffState.Active,
                CreatedAt = DateTime.UtcNow,
            });
            return;
        }

        existing.SupermarketId = supermarketId;
        existing.Status = PackagingStaffState.Active;
        repo.Update(existing);
    }

    public static async Task<PackagingStaff?> TryGetActiveMembershipAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return await unitOfWork.Repository<PackagingStaff>()
            .FirstOrDefaultAsync(ps => ps.UserId == userId && ps.Status == PackagingStaffState.Active);
    }

    public static async Task<Guid?> TryGetSupermarketIdAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await TryGetActiveMembershipAsync(unitOfWork, userId, cancellationToken);
        return row?.SupermarketId;
    }

    public static async Task<Guid> RequireSupermarketIdAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var supermarketId = await TryGetSupermarketIdAsync(unitOfWork, userId, cancellationToken);
        if (!supermarketId.HasValue)
            throw new InvalidOperationException(
                "Nhân viên đóng gói chưa được gán siêu thị. Vui lòng liên hệ quản trị viên.");

        return supermarketId.Value;
    }

    public static async Task ValidateSupermarketExistsAsync(
        IUnitOfWork unitOfWork,
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var supermarket = await unitOfWork.Repository<Supermarket>()
            .FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);

        if (supermarket == null)
            throw new InvalidOperationException("Siêu thị được chọn không tồn tại.");
    }
}
