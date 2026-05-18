using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Policies;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace CloseExpAISolution.Application.Services.Class;

public class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConnectionMultiplexer? _redis;
    private readonly PurchaseUnitOrderHelper _purchaseUnitHelper;
    private readonly IUnitConversionRateService _unitConversion;
    private static readonly ConcurrentDictionary<string, string> InMemoryCartStore = new();

    public CartService(
        IUnitOfWork unitOfWork,
        PurchaseUnitOrderHelper purchaseUnitHelper,
        IUnitConversionRateService unitConversion,
        IConnectionMultiplexer? redis = null)
    {
        _unitOfWork = unitOfWork;
        _purchaseUnitHelper = purchaseUnitHelper;
        _unitConversion = unitConversion;
        _redis = redis;
    }

    public async Task<CartResponseDto> GetMyCartAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cart = await LoadCartAsync(userId);
        return await MapCartAsync(userId, cart, cancellationToken);
    }

    public async Task<CartResponseDto> AddItemAsync(Guid userId, AddCartItemRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.");

        var (lot, product, units, purchaseUnitId, allowedUnitIds) =
            await LoadLotContextAsync(request.LotId, request.PurchaseUnitId, cancellationToken);

        _purchaseUnitHelper.EnsurePurchaseUnitAllowed(
            purchaseUnitId,
            product,
            lot,
            units,
            allowedUnitIds);

        var now = DateTime.UtcNow;
        EnsureLotOrderable(lot, now);

        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(x =>
            x.LotId == request.LotId && x.PurchaseUnitId == purchaseUnitId);

        var nextQuantity = (item?.Quantity ?? 0m) + request.Quantity;
        _purchaseUnitHelper.ValidateCartQuantityAgainstLot(lot, purchaseUnitId, nextQuantity, units);

        if (item == null)
        {
            cart.Items.Add(new RedisCartItem
            {
                LotId = request.LotId,
                PurchaseUnitId = purchaseUnitId,
                Quantity = request.Quantity
            });
        }
        else
        {
            item.Quantity = nextQuantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
        return await MapCartAsync(userId, cart, cancellationToken);
    }

    public async Task<CartResponseDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.");

        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(x => x.CartItemId == cartItemId)
            ?? throw new KeyNotFoundException("Không tìm thấy cart item.");

        var (lot, product, units, purchaseUnitId, allowedUnitIds) =
            await LoadLotContextAsync(item.LotId, item.PurchaseUnitId, cancellationToken);

        _purchaseUnitHelper.EnsurePurchaseUnitAllowed(
            purchaseUnitId,
            product,
            lot,
            units,
            allowedUnitIds);

        var now = DateTime.UtcNow;
        EnsureLotOrderable(lot, now);

        _purchaseUnitHelper.ValidateCartQuantityAgainstLot(lot, purchaseUnitId, request.Quantity, units);

        item.Quantity = request.Quantity;
        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
        return await MapCartAsync(userId, cart, cancellationToken);
    }

    public async Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken cancellationToken = default)
    {
        var cart = await LoadCartAsync(userId);
        var removed = cart.Items.RemoveAll(x => x.CartItemId == cartItemId);
        if (removed == 0)
            throw new KeyNotFoundException("Không tìm thấy cart item.");

        cart.UpdatedAt = DateTime.UtcNow;
        await SaveCartAsync(userId, cart);
        return await MapCartAsync(userId, cart, cancellationToken);
    }

    public async Task ClearAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = GetCartKey(userId);
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
            return;
        }

        InMemoryCartStore.TryRemove(key, out _);
    }

    private async Task<(
        StockLot Lot,
        Product Product,
        IReadOnlyDictionary<Guid, UnitConversionInfo> Units,
        Guid PurchaseUnitId,
        IReadOnlyList<Guid> AllowedUnitIds)> LoadLotContextAsync(
        Guid lotId,
        Guid? requestedPurchaseUnitId,
        CancellationToken cancellationToken)
    {
        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(x => x.LotId == lotId)
            ?? throw new KeyNotFoundException("Không tìm thấy lô hàng.");

        var product = await _unitOfWork.Repository<Product>().FirstOrDefaultAsync(x => x.ProductId == lot.ProductId)
            ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm.");

        var purchaseUnitId = PurchaseUnitOrderHelper.ResolvePurchaseUnitId(requestedPurchaseUnitId, lot);

        var now = DateTime.UtcNow;
        var publishedLots = (await _unitOfWork.Repository<StockLot>().FindAsync(l =>
            l.ProductId == product.ProductId
            && l.Status == ProductState.Published
            && l.Quantity > 0
            && l.ExpiryDate > now)).ToList();

        var allowedUnitIds = await _purchaseUnitHelper.GetAllowedPurchaseUnitIdsAsync(
            product,
            publishedLots,
            cancellationToken);

        var unitIds = allowedUnitIds
            .Concat(new[] { lot.UnitId, product.UnitId, purchaseUnitId })
            .Distinct();

        var units = await _unitConversion.LoadUnitInfoAsync(unitIds, cancellationToken);

        return (lot, product, units, purchaseUnitId, allowedUnitIds);
    }

    private static void EnsureLotOrderable(StockLot lot, DateTime now)
    {
        if (lot.Status != ProductState.Published || lot.Quantity <= 0 || lot.ExpiryDate <= now)
            throw new InvalidOperationException("Lô hàng không còn khả dụng để đặt.");

        if (DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(lot.ExpiryDate, now))
            throw new InvalidOperationException("Sau 21:00, không thể đặt lô hàng có hạn sử dụng trong ngày.");
    }

    private static string GetCartKey(Guid userId) => $"cart:{userId:D}";

    private async Task<RedisCart> LoadCartAsync(Guid userId)
    {
        var key = GetCartKey(userId);
        string? raw;
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            var redisRaw = await db.StringGetAsync(key);
            if (!redisRaw.HasValue)
                return new RedisCart();
            raw = redisRaw!;
        }
        else
        {
            if (!InMemoryCartStore.TryGetValue(key, out raw) || string.IsNullOrWhiteSpace(raw))
                return new RedisCart();
        }

        try
        {
            var cart = JsonSerializer.Deserialize<RedisCart>(raw) ?? new RedisCart();
            foreach (var item in cart.Items)
            {
                if (item.PurchaseUnitId == Guid.Empty)
                    item.PurchaseUnitId = null;
            }

            return cart;
        }
        catch
        {
            return new RedisCart();
        }
    }

    private async Task SaveCartAsync(Guid userId, RedisCart cart)
    {
        foreach (var item in cart.Items)
        {
            if (item.CartItemId == Guid.Empty)
                item.CartItemId = Guid.NewGuid();
        }

        var key = GetCartKey(userId);
        var raw = JsonSerializer.Serialize(cart);
        if (_redis != null)
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(key, raw, expiry: TimeSpan.FromDays(7));
            return;
        }

        InMemoryCartStore[key] = raw;
    }

    private async Task<CartResponseDto> MapCartAsync(Guid userId, RedisCart cart, CancellationToken cancellationToken)
    {
        var lotIds = cart.Items.Select(x => x.LotId).Distinct().ToList();
        var lots = lotIds.Count == 0
            ? new Dictionary<Guid, StockLot>()
            : (await _unitOfWork.Repository<StockLot>().FindAsync(x => lotIds.Contains(x.LotId))).ToDictionary(x => x.LotId);

        var productIds = lots.Values.Select(x => x.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _unitOfWork.Repository<Product>().FindAsync(x => productIds.Contains(x.ProductId)))
                .ToDictionary(x => x.ProductId);

        var lotUnitIds = lots.Values.Select(x => x.UnitId).Distinct().ToList();
        var lotUnits = lotUnitIds.Count == 0
            ? new Dictionary<Guid, UnitOfMeasure>()
            : (await _unitOfWork.Repository<UnitOfMeasure>().FindAsync(u => lotUnitIds.Contains(u.UnitId)))
                .ToDictionary(u => u.UnitId);

        var productUnitIds = products.Values.Select(p => p.UnitId).Distinct().ToList();
        var productUnits = productUnitIds.Count == 0
            ? new Dictionary<Guid, UnitOfMeasure>()
            : (await _unitOfWork.Repository<UnitOfMeasure>().FindAsync(u => productUnitIds.Contains(u.UnitId)))
                .ToDictionary(u => u.UnitId);

        var supermarkets = products.Values.Select(p => p.SupermarketId).Distinct().ToList();
        var supermarketEntities = supermarkets.Count == 0
            ? new Dictionary<Guid, Supermarket>()
            : (await _unitOfWork.Repository<Supermarket>().FindAsync(s => supermarkets.Contains(s.SupermarketId)))
                .ToDictionary(s => s.SupermarketId);

        var productImages = productIds.Count == 0
            ? new List<ProductImage>()
            : (await _unitOfWork.Repository<ProductImage>().FindAsync(pi => productIds.Contains(pi.ProductId)))
                .ToList();

        var purchaseUnitIds = cart.Items
            .Select(i => i.PurchaseUnitId ?? (lots.TryGetValue(i.LotId, out var l) ? l.UnitId : Guid.Empty))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var unitEntities = purchaseUnitIds.Count == 0
            ? new Dictionary<Guid, UnitOfMeasure>()
            : (await _unitOfWork.Repository<UnitOfMeasure>().FindAsync(u => purchaseUnitIds.Contains(u.UnitId)))
                .ToDictionary(u => u.UnitId);

        var unitIdsForConversion = cart.Items
            .SelectMany(i =>
            {
                if (!lots.TryGetValue(i.LotId, out var lot)) return Array.Empty<Guid>();
                var purchaseId = i.PurchaseUnitId ?? lot.UnitId;
                return new[] { lot.UnitId, purchaseId };
            })
            .Distinct();

        var units = await _unitConversion.LoadUnitInfoAsync(unitIdsForConversion, cancellationToken);

        var dtoItems = cart.Items.Select(i =>
        {
            lots.TryGetValue(i.LotId, out var lot);
            Product? product = null;
            if (lot != null)
                products.TryGetValue(lot.ProductId, out product);

            var purchaseUnitId = i.PurchaseUnitId ?? lot?.UnitId ?? Guid.Empty;
            unitEntities.TryGetValue(purchaseUnitId, out var purchaseUnitEntity);

            var lotUnitPrice = lot == null ? 0m : (lot.FinalUnitPrice ?? lot.SuggestedUnitPrice);
            var displayUnitPrice = lot == null || purchaseUnitId == Guid.Empty
                ? 0m
                : UnitConversionRateConverter.ConvertUnitPrice(
                    lot.UnitId,
                    purchaseUnitId,
                    lotUnitPrice,
                    units);

            var lineTotal = i.Quantity * displayUnitPrice;
            lotUnits.TryGetValue(lot?.UnitId ?? Guid.Empty, out var lotUnitEntity);
            productUnits.TryGetValue(product?.UnitId ?? Guid.Empty, out var productUnitEntity);
            supermarketEntities.TryGetValue(product?.SupermarketId ?? Guid.Empty, out var supermarketEntity);

            var imageUrl = product == null
                ? null
                : productImages
                    .Where(pi => pi.ProductId == product.ProductId)
                    .OrderByDescending(pi => pi.IsPrimary)
                    .ThenBy(pi => pi.CreatedAt)
                    .Select(pi => pi.ImageUrl)
                    .FirstOrDefault();

            return new CartItemResponseDto
            {
                CartItemId = i.CartItemId,
                LotId = i.LotId,
                PurchaseUnitId = purchaseUnitId == Guid.Empty ? null : purchaseUnitId,
                PurchaseUnitName = purchaseUnitEntity?.Name,
                PurchaseUnitSymbol = purchaseUnitEntity?.Symbol,
                ProductId = lot?.ProductId ?? Guid.Empty,
                ProductName = product?.Name ?? "N/A",
                ProductImageUrl = imageUrl,
                SupermarketId = product?.SupermarketId ?? Guid.Empty,
                SupermarketName = supermarketEntity?.Name,
                UnitId = lot?.UnitId ?? Guid.Empty,
                UnitName = lotUnitEntity?.Name,
                UnitSymbol = lotUnitEntity?.Symbol,
                ConversionRate = lotUnitEntity?.ConversionRate ?? 1m,
                ProductUnitId = product?.UnitId ?? Guid.Empty,
                ProductUnitName = productUnitEntity?.Name,
                ProductUnitSymbol = productUnitEntity?.Symbol,
                ProductConversionRate = productUnitEntity?.ConversionRate ?? 1m,
                ExpiryDate = lot?.ExpiryDate ?? DateTime.MinValue,
                Quantity = i.Quantity,
                UnitPrice = displayUnitPrice,
                LineTotal = lineTotal
            };
        }).ToList();

        return new CartResponseDto
        {
            CartId = Guid.Empty,
            UserId = userId,
            TotalItems = dtoItems.Count,
            TotalAmount = dtoItems.Sum(x => x.LineTotal),
            UpdatedAt = cart.UpdatedAt,
            Items = dtoItems
        };
    }

    private sealed class RedisCart
    {
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<RedisCartItem> Items { get; set; } = new();
    }

    private sealed class RedisCartItem
    {
        public Guid CartItemId { get; set; }
        public Guid LotId { get; set; }
        public Guid? PurchaseUnitId { get; set; }
        public decimal Quantity { get; set; }
    }
}
