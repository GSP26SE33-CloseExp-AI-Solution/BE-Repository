using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Policies;
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
    private static readonly ConcurrentDictionary<string, string> InMemoryCartStore = new();

    public CartService(IUnitOfWork unitOfWork, IConnectionMultiplexer? redis = null)
    {
        _unitOfWork = unitOfWork;
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

        var lot = await _unitOfWork.Repository<StockLot>().FirstOrDefaultAsync(x => x.LotId == request.LotId)
            ?? throw new KeyNotFoundException("Không tìm thấy lô hàng.");
        var now = DateTime.UtcNow;

        if (lot.Status != ProductState.Published || lot.Quantity <= 0 || lot.ExpiryDate <= now)
            throw new InvalidOperationException("Lô hàng không còn khả dụng để đặt.");

        if (DailyExpiryOrderingPolicy.IsLotBlockedForOrdering(lot.ExpiryDate, now))
            throw new InvalidOperationException("Sau 21:00, không thể đặt lô hàng có hạn sử dụng trong ngày.");

        var cart = await LoadCartAsync(userId);
        var item = cart.Items.FirstOrDefault(x => x.LotId == request.LotId);
        if (item == null)
        {
            cart.Items.Add(new RedisCartItem { LotId = request.LotId, Quantity = request.Quantity });
        }
        else
        {
            item.Quantity += request.Quantity;
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
            return JsonSerializer.Deserialize<RedisCart>(raw) ?? new RedisCart();
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
            : (await _unitOfWork.Repository<Product>().FindAsync(x => productIds.Contains(x.ProductId))).ToDictionary(x => x.ProductId);

        var dtoItems = cart.Items.Select(i =>
        {
            lots.TryGetValue(i.LotId, out var lot);
            Product? product = null;
            if (lot != null)
                products.TryGetValue(lot.ProductId, out product);

            var unitPrice = lot == null ? 0m : (lot.FinalUnitPrice ?? lot.SuggestedUnitPrice);
            var lineTotal = i.Quantity * unitPrice;
            return new CartItemResponseDto
            {
                CartItemId = i.CartItemId,
                LotId = i.LotId,
                ProductId = lot?.ProductId ?? Guid.Empty,
                ProductName = product?.Name ?? "N/A",
                ExpiryDate = lot?.ExpiryDate ?? DateTime.MinValue,
                Quantity = i.Quantity,
                UnitPrice = unitPrice,
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
        public decimal Quantity { get; set; }
     }
}
