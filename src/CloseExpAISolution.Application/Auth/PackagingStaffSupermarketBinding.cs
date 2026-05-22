using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.UnitOfWork;

namespace CloseExpAISolution.Application.Auth;

public static class PackagingStaffSupermarketBinding
{
    public static string ConfigKeyForUser(Guid userId) => $"{SystemConfigKeys.PackagingStaffSupermarketPrefix}{userId:D}";

    public static async Task UpsertAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        Guid supermarketId,
        CancellationToken cancellationToken = default)
    {
        var repo = unitOfWork.Repository<SystemConfig>();
        var key = ConfigKeyForUser(userId);
        var existing = await repo.FirstOrDefaultAsync(c => c.ConfigKey == key);

        if (existing == null)
        {
            await repo.AddAsync(new SystemConfig
            {
                ConfigKey = key,
                ConfigValue = supermarketId.ToString(),
                UpdatedAt = DateTime.UtcNow,
            });
            return;
        }

        existing.ConfigValue = supermarketId.ToString();
        existing.UpdatedAt = DateTime.UtcNow;
        repo.Update(existing);
    }

    public static async Task<Guid?> TryGetSupermarketIdAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cfg = await unitOfWork.Repository<SystemConfig>()
            .FirstOrDefaultAsync(c => c.ConfigKey == ConfigKeyForUser(userId));

        if (cfg == null || !Guid.TryParse(cfg.ConfigValue, out var supermarketId))
            return null;

        return supermarketId;
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
        var supermarket = await unitOfWork.Repository<Supermarket>()
            .FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);

        if (supermarket == null)
            throw new InvalidOperationException("Siêu thị được chọn không tồn tại.");
    }
}
