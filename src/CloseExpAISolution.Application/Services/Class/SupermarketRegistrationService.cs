using System.Security.Cryptography;
using System.Text;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace CloseExpAISolution.Application.Services.Class;

public class SupermarketRegistrationService : ISupermarketRegistrationService
{
    private const string MsgPendingOtherAccount = "Siêu thị này đang trong quá trình chờ xét duyệt hồ sơ từ một tài khoản khác.";
    private const string MsgActiveExists = "Siêu thị này đã được đăng ký trên hệ thống. Vui lòng liên hệ quản lý siêu thị để được thêm tài khoản nhân viên.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<SupermarketRegistrationService> _logger;

    public SupermarketRegistrationService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<SupermarketRegistrationService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<MySupermarketApplicationDto>> SubmitApplicationAsync(
        Guid vendorUserId,
        NewSupermarketRequest request,
        CancellationToken cancellationToken = default)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.FirstOrDefaultAsync(u => u.UserId == vendorUserId);
        if (user == null || user.RoleId != (int)RoleUser.Vendor || user.Status != UserState.Active)
            return ApiResponse<MySupermarketApplicationDto>.ErrorResponse("Chỉ tài khoản Vendor đang hoạt động mới có thể nộp hồ sơ mở siêu thị.");

        var supermarketRepo = _unitOfWork.Repository<Supermarket>();

        var ownPending = await supermarketRepo.FirstOrDefaultAsync(s =>
            s.ApplicantUserId == vendorUserId && s.Status == SupermarketState.PendingApproval);
        if (ownPending != null)
            return ApiResponse<MySupermarketApplicationDto>.SuccessResponse(MapMyDto(ownPending), "Bạn đã có hồ sơ đang chờ duyệt.");

        var ownActive = await supermarketRepo.FirstOrDefaultAsync(s =>
            s.ApplicantUserId == vendorUserId && s.Status == SupermarketState.Active);
        if (ownActive != null)
            return ApiResponse<MySupermarketApplicationDto>.ErrorResponse("Bạn đã có siêu thị đang hoạt động. Không thể mở thêm siêu thị mới theo chính sách hiện tại.");

        var blocking = await FindBlockingDuplicateAsync(request);
        if (blocking != null)
        {
            if (blocking.Status == SupermarketState.PendingApproval && blocking.ApplicantUserId != vendorUserId)
                return ApiResponse<MySupermarketApplicationDto>.ErrorResponse(MsgPendingOtherAccount);
            if (blocking.Status is SupermarketState.Active or SupermarketState.Suspended)
                return ApiResponse<MySupermarketApplicationDto>.ErrorResponse(MsgActiveExists);
        }

        var reference = await GenerateUniqueApplicationReferenceAsync();

        var supermarket = new Supermarket
        {
            SupermarketId = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ContactPhone = request.ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim(),
            Status = SupermarketState.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            ApplicantUserId = vendorUserId,
            SubmittedAt = DateTime.UtcNow,
            ApplicationReference = reference
        };

        await supermarketRepo.AddAsync(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await SendApplicationReceivedEmailAsync(user.Email, user.FullName, reference, supermarket.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send application received email for supermarket {SupermarketId}", supermarket.SupermarketId);
        }

        return ApiResponse<MySupermarketApplicationDto>.SuccessResponse(MapMyDto(supermarket), "Đã tiếp nhận hồ sơ đăng ký siêu thị.");
    }

    public async Task<ApiResponse<IReadOnlyList<MySupermarketApplicationDto>>> GetMyApplicationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        var list = (await supermarketRepo.FindAsync(s => s.ApplicantUserId == userId))
            .OrderByDescending(s => s.SubmittedAt ?? s.CreatedAt)
            .Select(MapMyDto)
            .ToList();
        return ApiResponse<IReadOnlyList<MySupermarketApplicationDto>>.SuccessResponse(list);
    }

    public async Task<ApiResponse<IReadOnlyList<AdminPendingSupermarketApplicationDto>>> GetPendingApplicationsForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        var userRepo = _unitOfWork.Repository<User>();
        var pending = (await supermarketRepo.FindAsync(s =>
                s.Status == SupermarketState.PendingApproval && s.ApplicantUserId != null))
            .OrderBy(s => s.SubmittedAt ?? s.CreatedAt)
            .ToList();

        var result = new List<AdminPendingSupermarketApplicationDto>();
        foreach (var s in pending)
        {
            User? applicant = null;
            if (s.ApplicantUserId.HasValue)
                applicant = await userRepo.FirstOrDefaultAsync(u => u.UserId == s.ApplicantUserId.Value);

            result.Add(new AdminPendingSupermarketApplicationDto
            {
                SupermarketId = s.SupermarketId,
                ApplicationReference = s.ApplicationReference,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                ContactPhone = s.ContactPhone,
                ContactEmail = s.ContactEmail,
                ApplicantUserId = s.ApplicantUserId,
                ApplicantEmail = applicant?.Email,
                ApplicantFullName = applicant?.FullName,
                SubmittedAt = s.SubmittedAt,
                CreatedAt = s.CreatedAt
            });
        }

        return ApiResponse<IReadOnlyList<AdminPendingSupermarketApplicationDto>>.SuccessResponse(result);
    }

    public async Task<ApiResponse<object>> ApproveApplicationAsync(
        Guid supermarketId,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        var userRepo = _unitOfWork.Repository<User>();
        var staffRepo = _unitOfWork.Repository<SupermarketStaff>();

        var supermarket = await supermarketRepo.FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);
        if (supermarket == null)
            return ApiResponse<object>.ErrorResponse("Không tìm thấy hồ sơ siêu thị.");
        if (supermarket.Status != SupermarketState.PendingApproval || supermarket.ApplicantUserId == null)
            return ApiResponse<object>.ErrorResponse("Hồ sơ không ở trạng thái chờ duyệt.");

        var applicant = await userRepo.FirstOrDefaultAsync(u => u.UserId == supermarket.ApplicantUserId.Value);
        if (applicant == null)
            return ApiResponse<object>.ErrorResponse("Không tìm thấy tài khoản người nộp đơn.");

        var employeeCode = GenerateNumericEmployeeCode();
        var hash = BCrypt.Net.BCrypt.HashPassword(employeeCode);
        var hint = employeeCode.Length >= 4 ? employeeCode[^4..] : employeeCode;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            supermarket.Status = SupermarketState.Active;
            supermarket.ReviewedAt = DateTime.UtcNow;
            supermarket.ReviewedByUserId = adminUserId;
            supermarket.AdminReviewNote = null;
            supermarketRepo.Update(supermarket);

            applicant.RoleId = (int)RoleUser.SupermarketStaff;
            applicant.UpdatedAt = DateTime.UtcNow;
            userRepo.Update(applicant);

            var managerStaff = new SupermarketStaff
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = applicant.UserId,
                SupermarketId = supermarket.SupermarketId,
                Position = "Quản lý",
                Status = SupermarketStaffState.Active,
                CreatedAt = DateTime.UtcNow,
                IsManager = true,
                EmployeeCodeHash = hash,
                EmployeeCodeHint = hint,
                ParentSuperStaffId = null
            };
            await staffRepo.AddAsync(managerStaff);

            await _unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Approve supermarket application failed for {SupermarketId}", supermarketId);
            return ApiResponse<object>.ErrorResponse("Duyệt hồ sơ thất bại. Vui lòng thử lại.");
        }

        try
        {
            await SendApplicationApprovedEmailAsync(applicant.Email, applicant.FullName, supermarket.Name, employeeCode, supermarket.ApplicationReference);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send approval email for supermarket {SupermarketId}", supermarketId);
        }

        return ApiResponse<object>.SuccessResponse(null!, "Đã duyệt và kích hoạt siêu thị. Người dùng cần làm mới phiên để đồng bộ vai trò.");
    }

    public async Task<ApiResponse<object>> RejectApplicationAsync(
        Guid supermarketId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        var userRepo = _unitOfWork.Repository<User>();

        var supermarket = await supermarketRepo.FirstOrDefaultAsync(s => s.SupermarketId == supermarketId);
        if (supermarket == null)
            return ApiResponse<object>.ErrorResponse("Không tìm thấy hồ sơ siêu thị.");
        if (supermarket.Status != SupermarketState.PendingApproval)
            return ApiResponse<object>.ErrorResponse("Hồ sơ không ở trạng thái chờ duyệt.");

        User? applicant = null;
        if (supermarket.ApplicantUserId.HasValue)
            applicant = await userRepo.FirstOrDefaultAsync(u => u.UserId == supermarket.ApplicantUserId.Value);

        supermarket.Status = SupermarketState.Rejected;
        supermarket.ReviewedAt = DateTime.UtcNow;
        supermarket.ReviewedByUserId = adminUserId;
        supermarket.AdminReviewNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        supermarketRepo.Update(supermarket);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (applicant != null)
        {
            try
            {
                await SendApplicationRejectedEmailAsync(applicant.Email, applicant.FullName, supermarket.Name, supermarket.AdminReviewNote, supermarket.ApplicationReference);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send rejection email for supermarket {SupermarketId}", supermarketId);
            }
        }

        return ApiResponse<object>.SuccessResponse(null!, "Đã từ chối hồ sơ.");
    }

    public async Task<ApiResponse<CreatedStaffPersonaDto>> CreateStaffPersonaAsync(
        Guid supermarketId,
        Guid currentUserId,
        Guid? jwtSupermarketStaffId,
        Guid? jwtSupermarketId,
        CreateStaffPersonaRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (jwtSupermarketStaffId == null || jwtSupermarketId == null || jwtSupermarketId != supermarketId)
            return ApiResponse<CreatedStaffPersonaDto>.ErrorResponse("Thiếu ngữ cảnh nhân viên trên token. Vui lòng đăng nhập lại và chọn mã nhân viên (quản lý).");

        var staffRepo = _unitOfWork.Repository<SupermarketStaff>();
        var managerRow = await staffRepo.FirstOrDefaultAsync(s => s.SupermarketStaffId == jwtSupermarketStaffId.Value);
        if (managerRow == null || managerRow.UserId != currentUserId || managerRow.SupermarketId != supermarketId)
            return ApiResponse<CreatedStaffPersonaDto>.ErrorResponse("Không xác định được quản lý siêu thị.");
        if (!managerRow.IsManager || managerRow.Status != SupermarketStaffState.Active)
            return ApiResponse<CreatedStaffPersonaDto>.ErrorResponse("Chỉ quản lý siêu thị mới được tạo thêm mã nhân viên.");

        var employeeCode = GenerateNumericEmployeeCode();
        var hash = BCrypt.Net.BCrypt.HashPassword(employeeCode);
        var hint = employeeCode.Length >= 4 ? employeeCode[^4..] : employeeCode;

        var newRow = new SupermarketStaff
        {
            SupermarketStaffId = Guid.NewGuid(),
            UserId = currentUserId,
            SupermarketId = supermarketId,
            Position = request.Position.Trim(),
            Status = SupermarketStaffState.Active,
            CreatedAt = DateTime.UtcNow,
            IsManager = false,
            EmployeeCodeHash = hash,
            EmployeeCodeHint = hint,
            ParentSuperStaffId = managerRow.SupermarketStaffId
        };

        await staffRepo.AddAsync(newRow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponse<CreatedStaffPersonaDto>.SuccessResponse(new CreatedStaffPersonaDto
        {
            SupermarketStaffId = newRow.SupermarketStaffId,
            SupermarketId = supermarketId,
            Position = newRow.Position,
            EmployeeCode = employeeCode,
            EmployeeCodeHint = hint
        }, "Đã tạo mã nhân viên mới. Lưu mã này an toàn — chỉ hiển thị một lần.");
    }

    private static MySupermarketApplicationDto MapMyDto(Supermarket s) => new()
    {
        SupermarketId = s.SupermarketId,
        ApplicationReference = s.ApplicationReference,
        Status = s.Status,
        Name = s.Name,
        Address = s.Address,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        ContactPhone = s.ContactPhone,
        ContactEmail = s.ContactEmail,
        SubmittedAt = s.SubmittedAt,
        ReviewedAt = s.ReviewedAt,
        AdminReviewNote = s.AdminReviewNote
    };

    private async Task<Supermarket?> FindBlockingDuplicateAsync(NewSupermarketRequest request)
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        var relevant = await supermarketRepo.FindAsync(s =>
            s.Status == SupermarketState.PendingApproval
            || s.Status == SupermarketState.Active
            || s.Status == SupermarketState.Suspended);

        var nName = NormalizeKeyPart(request.Name);
        var nAddr = NormalizeKeyPart(request.Address);
        return relevant.FirstOrDefault(s =>
            NormalizeKeyPart(s.Name) == nName
            && NormalizeKeyPart(s.Address) == nAddr
            && s.Latitude == request.Latitude
            && s.Longitude == request.Longitude);
    }

    private static string NormalizeKeyPart(string value) => value.Trim().ToLowerInvariant();

    private async Task<string> GenerateUniqueApplicationReferenceAsync()
    {
        var supermarketRepo = _unitOfWork.Repository<Supermarket>();
        string reference;
        var attempts = 0;
        do
        {
            reference = $"ST-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberToString(6)}";
            var exists = await supermarketRepo.ExistsAsync(s => s.ApplicationReference == reference);
            if (!exists)
                return reference;
            attempts++;
        } while (attempts < 20);

        return $"ST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}";
    }

    private static string RandomNumberToString(int digits)
    {
        using var rng = RandomNumberGenerator.Create();
        var buf = new byte[4];
        rng.GetBytes(buf);
        var n = Math.Abs(BitConverter.ToUInt32(buf, 0)) % (uint)Math.Pow(10, digits);
        return n.ToString().PadLeft(digits, '0');
    }

    private static string GenerateNumericEmployeeCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var buf = new byte[4];
        rng.GetBytes(buf);
        var n = Math.Abs(BitConverter.ToUInt32(buf, 0)) % 900_000u + 100_000u;
        return n.ToString();
    }

    private async Task SendApplicationReceivedEmailAsync(string email, string fullName, string? reference, string marketName)
    {
        var subject = "CloseExp — Đã tiếp nhận hồ sơ mở siêu thị";
        var body = new StringBuilder()
            .Append("<html><body style='font-family:Arial,sans-serif'>")
            .Append("<p>Xin chào ").Append(WebEncode(fullName)).Append(",</p>")
            .Append("<p>Chúng tôi đã nhận hồ sơ đăng ký siêu thị <strong>")
            .Append(WebEncode(marketName)).Append("</strong>.</p>")
            .Append("<p>Mã hồ sơ: <strong>").Append(WebEncode(reference ?? "")).Append("</strong></p>")
            .Append("<p>Bạn sẽ nhận email khi hồ sơ được duyệt hoặc từ chối.</p>")
            .Append("</body></html>")
            .ToString();
        await _emailService.SendEmailAsync(email, subject, body, CancellationToken.None);
    }

    private async Task SendApplicationApprovedEmailAsync(string email, string fullName, string marketName, string employeeCode, string? reference)
    {
        var subject = "CloseExp — Hồ sơ siêu thị đã được duyệt";
        var body = new StringBuilder()
            .Append("<html><body style='font-family:Arial,sans-serif'>")
            .Append("<p>Xin chào ").Append(WebEncode(fullName)).Append(",</p>")
            .Append("<p>Chúc mừng! Siêu thị <strong>").Append(WebEncode(marketName))
            .Append("</strong> đã được kích hoạt. Vai trò tài khoản đã cập nhật thành nhân viên siêu thị.</p>")
            .Append("<p>Mã hồ sơ: ").Append(WebEncode(reference ?? "")).Append("</p>")
            .Append("<p><strong>Mã nhân viên quản lý (lưu kỹ, chỉ hiển thị lần này):</strong> ")
            .Append(WebEncode(employeeCode)).Append("</p>")
            .Append("<p>Vui lòng <strong>làm mới phiên đăng nhập</strong> (refresh token) để đồng bộ quyền truy cập.</p>")
            .Append("</body></html>")
            .ToString();
        await _emailService.SendEmailAsync(email, subject, body, CancellationToken.None);
    }

    private async Task SendApplicationRejectedEmailAsync(string email, string fullName, string marketName, string? note, string? reference)
    {
        var subject = "CloseExp — Hồ sơ siêu thị chưa được duyệt";
        var body = new StringBuilder()
            .Append("<html><body style='font-family:Arial,sans-serif'>")
            .Append("<p>Xin chào ").Append(WebEncode(fullName)).Append(",</p>")
            .Append("<p>Rất tiếc, hồ sơ siêu thị <strong>").Append(WebEncode(marketName))
            .Append("</strong> chưa được phê duyệt.</p>")
            .Append("<p>Mã hồ sơ: ").Append(WebEncode(reference ?? "")).Append("</p>");
        if (!string.IsNullOrWhiteSpace(note))
            body.Append("<p>Ghi chú từ quản trị: ").Append(WebEncode(note)).Append("</p>");
        body.Append("<p>Bạn có thể liên hệ hỗ trợ để biết thêm chi tiết.</p></body></html>");
        await _emailService.SendEmailAsync(email, subject, body.ToString(), CancellationToken.None);
    }

    private static string WebEncode(string s) =>
        System.Net.WebUtility.HtmlEncode(s);
}
