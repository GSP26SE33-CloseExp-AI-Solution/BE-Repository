using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ICartService
{
    Task<CartResponseDto> GetMyCartAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CartResponseDto> AddItemAsync(Guid userId, AddCartItemRequestDto request, CancellationToken cancellationToken = default);
    Task<CartResponseDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequestDto request, CancellationToken cancellationToken = default);
    Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid cartItemId, CancellationToken cancellationToken = default);
    Task ClearAsync(Guid userId, CancellationToken cancellationToken = default);
}
