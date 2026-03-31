using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;

namespace CloseExpAISolution.Application.Services.Interface;

public interface ISupermarketRegistrationService
{
    Task<ApiResponse<MySupermarketApplicationDto>> SubmitApplicationAsync(Guid vendorUserId, NewSupermarketRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<MySupermarketApplicationDto>>> GetMyApplicationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<AdminPendingSupermarketApplicationDto>>> GetPendingApplicationsForAdminAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> ApproveApplicationAsync(Guid supermarketId, Guid adminUserId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> RejectApplicationAsync(Guid supermarketId, Guid adminUserId, string? adminNote, CancellationToken cancellationToken = default);
    Task<ApiResponse<CreatedStaffPersonaDto>> CreateStaffPersonaAsync(
        Guid supermarketId,
        Guid currentUserId,
        Guid? jwtSupermarketStaffId,
        Guid? jwtSupermarketId,
        CreateStaffPersonaRequestDto request,
        CancellationToken cancellationToken = default);
}
