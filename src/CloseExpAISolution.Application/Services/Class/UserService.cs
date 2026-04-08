using AutoMapper;
using CloseExpAISolution.Application.DTOs;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService, ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
        _logger = logger;
    }

    #region Public Methods

    public async Task<ApiResponse<IEnumerable<UserResponseDto>>> GetAllUsersAsync()
    {
        var users = await _unitOfWork.Repository<User>().GetAllAsync();
        var filteredUsers = users.Where(u => u.RoleId != (int)RoleUser.Admin);
        var roleDictionary = await GetRoleDictionary();

        var userResponses = new List<UserResponseDto>();
        foreach (var user in filteredUsers)
        {
            var dto = await MapUserWithRoleAndStaffInfoAsync(user, roleDictionary);
            userResponses.Add(dto);
        }

        return ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(userResponses);
    }

    public async Task<ApiResponse<UserResponseDto>> GetUserByIdAsync(Guid id)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse);
    }

    public async Task<ApiResponse<UserResponseDto>> CreateUserAsync(CreateUserRequestDto request)
    {
        // Validate email uniqueness
        if (await EmailExists(request.Email))
            return Error("Email đã tồn tại");

        // Validate role
        var role = await GetRoleById(request.RoleId);
        if (role == null)
            return Error("Vai trò không hợp lệ");

        // Create user
        var user = _mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _unitOfWork.Repository<User>().AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var userResponse = _mapper.Map<UserResponseDto>(user);
        userResponse.RoleName = role.RoleName;

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Tạo người dùng thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserAsync(Guid id, UpdateUserRequestDto request)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        // Validate email change
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (await EmailExists(request.Email))
                return Error("Email đã tồn tại");
            user.Email = request.Email;
        }

        // Validate role change
        if (request.RoleId.HasValue)
        {
            var role = await GetRoleById(request.RoleId.Value);
            if (role == null)
                return Error("Vai trò không hợp lệ");
            user.RoleId = request.RoleId.Value;
        }

        // Update basic fields
        UpdateUserFields(user, request.FullName, request.Phone);

        if (request.Status.HasValue)
        {
            var statusValidation = ValidateStatusTransition(user, request.Status.Value);
            if (statusValidation != null)
                return statusValidation;

            user.Status = request.Status.Value;
        }

        await SaveUserChanges(user);

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Cập nhật người dùng thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequestDto request)
    {
        var user = await FindUserById(userId);
        if (user == null)
            return NotFound();

        UpdateUserFields(user, request.FullName, request.Phone);
        await SaveUserChanges(user);

        var userResponse = await MapUserWithRoleAsync(user);
        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, "Cập nhật thông tin cá nhân thành công");
    }

    public async Task<ApiResponse<UserResponseDto>> UpdateUserStatusAsync(Guid id, UpdateUserStatusRequestDto request)
    {
        var user = await FindUserById(id);
        if (user == null)
            return NotFound();

        var oldStatus = user.Status;

        if (oldStatus == request.Status)
            return Error($"Tài khoản đã ở trạng thái {request.Status}");

        var statusValidation = ValidateStatusTransition(user, request.Status);
        if (statusValidation != null)
            return statusValidation;

        user.Status = request.Status;

        // Reset FailedLoginCount khi Active / Admin lock
        if (request.Status == UserState.Active || request.Status == UserState.Locked)
            user.FailedLoginCount = 0;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<User>().Update(user);

            // Revoke tất cả refresh token khi lock/ban
            if (request.Status == UserState.Locked || request.Status == UserState.Banned)
            {
                await RevokeAllUserTokensInternalAsync(user.UserId);
            }

            // Auto-hide sản phẩm siêu thị khi ban SupermarketStaff; hủy đơn đang mở có lô thuộc các siêu thị đó
            if (request.Status == UserState.Banned && user.RoleId == (int)RoleUser.SupermarketStaff)
            {
                var supermarketIds = await HideSupermarketProductsAsync(user.UserId);
                await CancelOpenOrdersForSupermarketsAsync(supermarketIds);
            }
            // TODO: Xử lý MarketingStaff khi có entity liên kết với Supermarket

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to update status for user {UserId}", id);
            return Error("Cập nhật trạng thái thất bại. Vui lòng thử lại sau");
        }

        await SendStatusChangeEmailAsync(user, oldStatus, request.Status);
        var userResponse = await MapUserWithRoleAsync(user);
        var statusMessage = GetStatusChangeMessage(oldStatus.ToString(), request.Status.ToString());

        return ApiResponse<UserResponseDto>.SuccessResponse(userResponse, statusMessage);
    }

    public async Task<ApiResponse<bool>> DeleteUserAsync(Guid id)
    {
        var user = await FindUserById(id);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await RevokeAllUserTokensInternalAsync(user.UserId);

            user.Status = UserState.Deleted;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<User>().Update(user);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to delete user {UserId}", id);
            return ApiResponse<bool>.ErrorResponse("Xóa người dùng thất bại. Vui lòng thử lại sau");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa người dùng thành công");
    }

    public async Task<ApiResponse<bool>> DeleteOwnAccountAsync(Guid userId)
    {
        var user = await FindUserById(userId);
        if (user == null)
            return ApiResponse<bool>.ErrorResponse("Không tìm thấy người dùng");

        // Nhân viên siêu thị không thể tự xóa tài khoản
        if (user.RoleId == (int)RoleUser.SupermarketStaff)
            return ApiResponse<bool>.ErrorResponse("Nhân viên siêu thị không thể tự xóa tài khoản. Vui lòng liên hệ quản trị viên");
        if (user.RoleId != (int)RoleUser.Vendor)
            return ApiResponse<bool>.ErrorResponse("Bạn không có quyền xóa tài khoản này. Vui lòng liên hệ Admin");

        if (user.Status == UserState.Deleted)
            return ApiResponse<bool>.ErrorResponse("Tài khoản đã bị xóa trước đó");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Soft delete user
            user.Status = UserState.Deleted;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<User>().Update(user);

            // Revoke tất cả refresh token
            var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
            var activeTokens = await refreshTokenRepo.FindAsync(t => t.UserId == userId && t.RevokedAt == null);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                refreshTokenRepo.Update(token);
            }

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Failed to delete own account for user {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse("Xóa tài khoản thất bại. Vui lòng thử lại sau");
        }

        return ApiResponse<bool>.SuccessResponse(true, "Xóa tài khoản thành công");
    }

    #endregion

    #region Private Helpers

    private async Task<User?> FindUserById(Guid id)
        => await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.UserId == id);

    private async Task<bool> EmailExists(string email)
    {
        var existingUser = await _unitOfWork.Repository<User>().FirstOrDefaultAsync(u => u.Email == email);
        return existingUser != null;
    }

    private async Task<Role?> GetRoleById(int roleId)
        => await _unitOfWork.Repository<Role>().GetByIdAsync(roleId);

    private async Task<Dictionary<int, string>> GetRoleDictionary()
    {
        var roles = await _unitOfWork.Repository<Role>().GetAllAsync();
        return roles.ToDictionary(r => r.RoleId, r => r.RoleName);
    }

    private async Task<UserResponseDto> MapUserWithRoleAsync(User user)
    {
        var role = await GetRoleById(user.RoleId);
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = role?.RoleName ?? "Unknown";

        // Nếu là SupermarketStaff (RoleId = 4) - nhân viên siêu thị thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.SupermarketStaff)
        {
            var memberships = await GetMarketStaffMembershipsAsync(user.UserId);
            dto.MarketStaffMemberships = memberships;
            dto.MarketStaffInfo = PickPrimaryMarketStaffInfo(memberships);
        }

        return dto;
    }

    private UserResponseDto MapUserWithRole(User user, Dictionary<int, string> roleDictionary)
    {
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = roleDictionary.GetValueOrDefault(user.RoleId, "Unknown");
        return dto;
    }

    private async Task<UserResponseDto> MapUserWithRoleAndStaffInfoAsync(User user, Dictionary<int, string> roleDictionary)
    {
        var dto = _mapper.Map<UserResponseDto>(user);
        dto.RoleName = roleDictionary.GetValueOrDefault(user.RoleId, "Unknown");

        // Nếu là SupermarketStaff (nhân viên siêu thị) thì load thông tin siêu thị
        if (user.RoleId == (int)RoleUser.SupermarketStaff)
        {
            var memberships = await GetMarketStaffMembershipsAsync(user.UserId);
            dto.MarketStaffMemberships = memberships;
            dto.MarketStaffInfo = PickPrimaryMarketStaffInfo(memberships);
        }

        return dto;
    }

    private async Task<List<MarketStaffInfoDto>> GetMarketStaffMembershipsAsync(Guid userId)
    {
        var staffRepo = _unitOfWork.Repository<SupermarketStaff>();
        var marketRepo = _unitOfWork.Repository<Supermarket>();
        var rows = (await staffRepo.FindAsync(ms =>
                ms.UserId == userId && ms.Status == SupermarketStaffState.Active))
            .OrderByDescending(ms => ms.IsManager)
            .ThenBy(ms => ms.CreatedAt)
            .ToList();

        var list = new List<MarketStaffInfoDto>();
        foreach (var ms in rows)
        {
            var supermarket = await marketRepo.FirstOrDefaultAsync(s => s.SupermarketId == ms.SupermarketId);
            list.Add(new MarketStaffInfoDto
            {
                MarketStaffId = ms.SupermarketStaffId,
                Position = ms.Position,
                JoinedAt = ms.CreatedAt,
                IsManager = ms.IsManager,
                EmployeeCodeHint = ms.EmployeeCodeHint,
                Supermarket = supermarket == null
                    ? null
                    : new SupermarketBasicInfoDto
                    {
                        SupermarketId = supermarket.SupermarketId,
                        Name = supermarket.Name,
                        Address = supermarket.Address,
                        ContactPhone = supermarket.ContactPhone
                    }
            });
        }

        return list;
    }

    private static MarketStaffInfoDto? PickPrimaryMarketStaffInfo(IReadOnlyList<MarketStaffInfoDto> memberships)
    {
        if (memberships.Count == 0)
            return null;
        return memberships.FirstOrDefault(m => m.IsManager) ?? memberships[0];
    }

    private async Task RevokeAllUserTokensInternalAsync(Guid userId)
    {
        var refreshTokenRepo = _unitOfWork.Repository<RefreshToken>();
        var activeTokens = await refreshTokenRepo.FindAsync(t => t.UserId == userId && t.RevokedAt == null);
        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            refreshTokenRepo.Update(token);
        }
    }

    /// <summary>
    /// Ẩn sản phẩm (Hidden) trên mọi siêu thị mà nhân viên đang gắn; trả về danh sách siêu thị để xử lý đơn.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> HideSupermarketProductsAsync(Guid userId)
    {
        var staffRows = await _unitOfWork.Repository<SupermarketStaff>().FindAsync(ms => ms.UserId == userId);
        var supermarketIds = staffRows.Select(s => s.SupermarketId).Distinct().ToList();
        if (supermarketIds.Count == 0)
            return supermarketIds;

        var totalHidden = 0;
        foreach (var smId in supermarketIds)
        {
            var products = await _unitOfWork.Repository<Product>()
                .FindAsync(p => p.SupermarketId == smId
                               && p.Status != ProductState.Hidden
                               && p.Status != ProductState.Deleted);

            foreach (var product in products)
            {
                product.Status = ProductState.Hidden;
                product.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Product>().Update(product);
            }

            totalHidden += products.Count();
        }

        _logger.LogInformation(
            "Auto-hidden {Count} products across {SupermarketCount} supermarkets due to staff {UserId} being banned",
            totalHidden, supermarketIds.Count, userId);

        return supermarketIds;
    }

    /// <summary>
    /// Hủy các đơn chưa kết thúc nếu đơn chứa ít nhất một dòng hàng thuộc lô của sản phẩm siêu thị trong danh sách.
    /// </summary>
    private async Task CancelOpenOrdersForSupermarketsAsync(IReadOnlyList<Guid> supermarketIds)
    {
        if (supermarketIds.Count == 0)
            return;

        var terminal = new[]
        {
            OrderState.Completed,
            OrderState.Canceled,
            OrderState.Refunded,
            OrderState.Failed
        };

        var productIds = (await _unitOfWork.Repository<Product>()
                .FindAsync(p => supermarketIds.Contains(p.SupermarketId)))
            .Select(p => p.ProductId)
            .ToHashSet();

        if (productIds.Count == 0)
            return;

        var lotIds = (await _unitOfWork.Repository<StockLot>()
                .FindAsync(l => productIds.Contains(l.ProductId)))
            .Select(l => l.LotId)
            .ToHashSet();

        if (lotIds.Count == 0)
            return;

        var orderItems = await _unitOfWork.Repository<OrderItem>().FindAsync(oi => lotIds.Contains(oi.LotId));
        var orderIds = orderItems.Select(oi => oi.OrderId).Distinct().ToList();
        if (orderIds.Count == 0)
            return;

        var orders = await _unitOfWork.Repository<Order>().FindAsync(o => orderIds.Contains(o.OrderId));
        var orderRepo = _unitOfWork.Repository<Order>();
        var ordersToCancel = orders.Where(o => !terminal.Contains(o.Status)).ToList();
        var now = DateTime.UtcNow;

        var orderIdsToRestore = ordersToCancel
            .Where(o => o.Status != OrderState.Pending)
            .Select(o => o.OrderId)
            .ToHashSet();

        if (orderIdsToRestore.Count > 0)
        {
            var restoreItems = orderItems
                .Where(oi => orderIdsToRestore.Contains(oi.OrderId))
                .ToList();

            if (restoreItems.Count > 0)
            {
                var requiredByLot = restoreItems
                    .GroupBy(oi => oi.LotId)
                    .Select(g => new
                    {
                        LotId = g.Key,
                        RequiredQuantity = (decimal)g.Sum(x => x.Quantity)
                    })
                    .ToList();

                var restoreLotIds = requiredByLot.Select(x => x.LotId).ToList();
                var lots = await _unitOfWork.Repository<StockLot>().FindAsync(l => restoreLotIds.Contains(l.LotId));
                var lotById = lots.ToDictionary(l => l.LotId);

                foreach (var req in requiredByLot)
                {
                    if (!lotById.TryGetValue(req.LotId, out var lot))
                        throw new InvalidOperationException(
                            $"Không tìm thấy StockLot {req.LotId} để hoàn kho cho các đơn đã bị hủy.");

                    lot.Quantity += req.RequiredQuantity;
                    lot.UpdatedAt = now;
                    _unitOfWork.Repository<StockLot>().Update(lot);
                }
            }
        }

        foreach (var order in ordersToCancel)
        {
            var oldStatus = order.Status;
            order.Status = OrderState.Canceled;
            order.UpdatedAt = now;
            orderRepo.Update(order);

            var log = new OrderStatusLog
            {
                LogId = Guid.NewGuid(),
                OrderId = order.OrderId,
                FromStatus = oldStatus,
                ToStatus = OrderState.Canceled,
                ChangedBy = "system",
                Note = "Canceled: supermarket staff banned (products hidden)",
                ChangedAt = now
            };
            await _unitOfWork.Repository<OrderStatusLog>().AddAsync(log);

            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = order.UserId,
                Title = "Đơn hàng đã bị hủy",
                Content = $"Đơn {order.OrderCode} đã bị hủy do siêu thị ngừng cung cấp (tài khoản nhân viên bị cấm).",
                Type = NotificationType.OrderUpdate,
                IsRead = false,
                CreatedAt = now
            };
            await _unitOfWork.Repository<Notification>().AddAsync(notification);
        }

        _logger.LogInformation(
            "Canceled open orders tied to supermarkets {SupermarketIds} after staff ban",
            string.Join(',', supermarketIds));
    }

    private static void UpdateUserFields(User user, string? fullName, string? phone)
    {
        if (!string.IsNullOrEmpty(fullName))
            user.FullName = fullName;

        if (!string.IsNullOrEmpty(phone))
            user.Phone = phone;
    }

    private async Task SaveUserChanges(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private static ApiResponse<UserResponseDto>? ValidateStatusTransition(User user, UserState newStatus)
    {
        if (newStatus != UserState.Active)
            return null;

        if (!Enum.TryParse<UserState>(user.Status.ToString(), out var currentStatus))
            return ApiResponse<UserResponseDto>.ErrorResponse("Trạng thái hiện tại của tài khoản không hợp lệ");

        if (currentStatus != UserState.PendingApproval && currentStatus != UserState.Banned)
            return ApiResponse<UserResponseDto>.ErrorResponse(
                "Chỉ có thể chuyển sang Active từ trạng thái PendingApproval hoặc Banned");

        if (currentStatus == UserState.PendingApproval && user.EmailVerifiedAt == null)
            return ApiResponse<UserResponseDto>.ErrorResponse(
                "Tài khoản chưa xác minh email, không thể chuyển sang Active");

        return null;
    }

    private static string GetStatusChangeMessage(string oldStatus, string newStatus) => newStatus switch
    {
        nameof(UserState.Active) => "Kích hoạt tài khoản thành công",
        nameof(UserState.PendingApproval) => "Chuyển tài khoản sang chờ phê duyệt",
        nameof(UserState.Rejected) => "Từ chối phê duyệt tài khoản",
        nameof(UserState.Locked) => "Khóa tạm thời tài khoản thành công",
        nameof(UserState.Banned) => "Cấm vĩnh viễn tài khoản thành công",
        nameof(UserState.Unverified) => "Hủy xác minh tài khoản thành công",
        nameof(UserState.Hidden) => "Ẩn tài khoản thành công",
        nameof(UserState.Deleted) => "Xóa tài khoản thành công",
        _ => $"Cập nhật trạng thái từ {oldStatus} sang {newStatus} thành công"
    };

    private static ApiResponse<UserResponseDto> NotFound()
        => ApiResponse<UserResponseDto>.ErrorResponse("Không tìm thấy người dùng");

    private static ApiResponse<UserResponseDto> Error(string message)
        => ApiResponse<UserResponseDto>.ErrorResponse(message);

    private async Task SendStatusChangeEmailAsync(User user, UserState oldStatus, UserState newStatus)
    {
        try
        {
            string? subject = null;
            string? body = null;

            if (newStatus == UserState.Active && oldStatus == UserState.PendingApproval)
            {
                subject = "CloseExp AI - Tài khoản đã được phê duyệt!";
                body = BuildEmailBody(
                    "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)",
                    "Tài Khoản Đã Được Phê Duyệt!",
                    user.FullName,
                    "Tài khoản của bạn đã được <strong>Admin phê duyệt thành công</strong>.",
                    "Bạn có thể đăng nhập vào hệ thống CloseExp AI ngay bây giờ!");
            }
            else if (newStatus == UserState.Active &&
                     (oldStatus == UserState.Locked || oldStatus == UserState.Banned))
            {
                subject = "CloseExp AI - Tài khoản đã được mở khóa!";
                body = BuildEmailBody(
                    "linear-gradient(135deg, #11998e 0%, #38ef7d 100%)",
                    "Tài Khoản Đã Được Mở Khóa!",
                    user.FullName,
                    "Tài khoản của bạn đã được <strong>Admin mở khóa thành công</strong>.",
                    "Bạn có thể đăng nhập vào hệ thống CloseExp AI ngay bây giờ!");
            }
            else if (newStatus == UserState.Rejected)
            {
                subject = "CloseExp AI - Tài khoản không được phê duyệt";
                body = BuildEmailBody(
                    "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                    "Thông Báo Về Tài Khoản",
                    user.FullName,
                    "Rất tiếc, tài khoản của bạn <strong>không được phê duyệt</strong> bởi Admin.",
                    "Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ với đội ngũ hỗ trợ.");
            }
            else if (newStatus == UserState.Locked)
            {
                subject = "CloseExp AI - Tài khoản đã bị khóa tạm thời";
                body = BuildEmailBody(
                    "linear-gradient(135deg, #f093fb 0%, #f5576c 100%)",
                    "Tài Khoản Bị Khóa Tạm Thời",
                    user.FullName,
                    "Tài khoản của bạn đã bị <strong>khóa tạm thời</strong> bởi Admin.",
                    "Nếu bạn cho rằng đây là nhầm lẫn, vui lòng đăng nhập và nhấn nút yêu cầu mở khóa hoặc liên hệ đội ngũ hỗ trợ.");
            }
            else if (newStatus == UserState.Banned)
            {
                subject = "CloseExp AI - Tài khoản đã bị cấm vĩnh viễn";
                body = BuildEmailBody(
                    "linear-gradient(135deg, #e74c3c 0%, #c0392b 100%)",
                    "Tài Khoản Bị Cấm Vĩnh Viễn",
                    user.FullName,
                    "Tài khoản của bạn đã bị <strong>cấm vĩnh viễn</strong> bởi Admin.",
                    "Nếu bạn cho rằng đây là nhầm lẫn, vui lòng liên hệ Admin qua email để được hỗ trợ.");
            }

            if (subject != null && body != null)
                await _emailService.SendEmailAsync(user.Email, subject, body, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send status change email to {Email}", user.Email);
        }
    }

    private static string BuildEmailBody(string gradient, string title, string fullName, string mainMessage, string subMessage)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: {gradient}; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
                    <h1 style='color: white; margin: 0;'>{title}</h1>
                </div>
                <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
                    <h2>Xin chào {fullName}!</h2>
                    <p>{mainMessage}</p>
                    <p>{subMessage}</p>
                    <p style='color: #999; font-size: 12px;'>CloseExp AI Team</p>
                </div>
            </body>
            </html>";
    }

    #endregion
}



