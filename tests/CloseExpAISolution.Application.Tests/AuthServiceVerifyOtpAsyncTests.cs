using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN003 — <c>.github/instructions/verify-otp-async-test-sheet.md</c> (UTCID01–UTCID06).
/// <see cref="AuthService.VerifyOtpAsync"/>.
/// Run: <c>dotnet test --filter "FullyQualifiedName~AuthServiceVerifyOtpAsyncTests"</c>
/// </summary>
public sealed class AuthServiceVerifyOtpAsyncTests
{
    private const string ValidOtpPlain = "123456";

    /// <summary>Matches <see cref="AuthService"/> private HashOtp logic.</summary>
    private static string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToBase64String(bytes);
    }

    private static (SqliteConnection Conn, ApplicationDbContext Ctx) CreateSqliteContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conn)
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();
        return (conn, ctx);
    }

    /// <summary>SQLite enforces FK from <see cref="User.RoleId"/> to <c>Roles</c>.</summary>
    private static async Task SeedRolesForOtpTestsAsync(ApplicationDbContext ctx)
    {
        ctx.Roles.AddRange(
            new Role { RoleId = (int)RoleUser.SupermarketStaff, RoleName = "SupermarketStaff" },
            new Role { RoleId = (int)RoleUser.DeliveryStaff, RoleName = "DeliveryStaff" },
            new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
        await ctx.SaveChangesAsync();
    }

    private static AuthService CreateSut(IUnitOfWork uow, ILogger<AuthService>? logger = null) =>
        new AuthService(
            uow,
            new ConfigurationBuilder().Build(),
            Mock.Of<IEmailService>(e =>
                e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()) ==
                Task.CompletedTask),
            logger ?? new Mock<ILogger<AuthService>>().Object,
            Mock.Of<IMapper>(),
            Mock.Of<IMapboxService>());

    private static VerifyOtpRequest Request(string email, string otp) =>
        new() { Email = email, OtpCode = otp };

    private static User CreateUnverifiedUser(
        Guid userId,
        string email,
        int roleId,
        string? otpHash = null,
        DateTime? otpExpiresAt = null,
        int otpFailedCount = 0)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            UserId = userId,
            Email = email,
            FullName = "OTP User",
            Phone = "0902222222",
            PasswordHash = "x",
            RoleId = roleId,
            Status = UserState.Unverified,
            FailedLoginCount = 0,
            OtpCode = otpHash,
            OtpExpiresAt = otpExpiresAt,
            OtpFailedCount = otpFailedCount,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void VerifyInformationLogContains(Mock<ILogger<AuthService>> logger, string substring, Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v != null && v.ToString()!.Contains(substring, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task UTCID01_VendorCorrectOtp_ActivatesUser_LogsInformation()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f03-0001-0001-000000000001");
            var email = "vendor-otp@local.test";
            var expires = DateTime.UtcNow.AddHours(1);

            await SeedRolesForOtpTestsAsync(ctx);
            ctx.Users.Add(CreateUnverifiedUser(
                uid,
                email,
                (int)RoleUser.Vendor,
                otpHash: HashOtp(ValidOtpPlain),
                otpExpiresAt: expires));
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var logger = new Mock<ILogger<AuthService>>();
            var sut = CreateSut(new UnitOfWork(ctx), logger.Object);

            var res = await sut.VerifyOtpAsync(Request(email, ValidOtpPlain));

            Assert.True(res.Success);
            Assert.True(res.Data);
            Assert.Contains("đăng nhập ngay", res.Message, StringComparison.OrdinalIgnoreCase);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(u => u.UserId == uid);
            Assert.Equal(UserState.Active, stored.Status);
            Assert.Null(stored.OtpCode);
            Assert.Null(stored.OtpExpiresAt);
            Assert.Equal(0, stored.OtpFailedCount);

            VerifyInformationLogContains(logger, "Dữ liệu user", Times.Once());
        }
    }

    public static TheoryData<int, string> InternalRoleAndExpectedSubstring =>
        new()
        {
            { (int)RoleUser.SupermarketStaff, "phê duyệt" },
            { (int)RoleUser.DeliveryStaff, "đăng nhập ngay" }
        };

    [Theory]
    [MemberData(nameof(InternalRoleAndExpectedSubstring))]
    public async Task UTCID02_InternalCorrectOtp_SetsPendingApproval_AndMessageByRole(int roleId, string expectedSubstring)
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.NewGuid();
            var email = $"internal-{roleId}@local.test";
            await SeedRolesForOtpTestsAsync(ctx);
            ctx.Users.Add(CreateUnverifiedUser(
                uid,
                email,
                roleId,
                otpHash: HashOtp(ValidOtpPlain),
                otpExpiresAt: DateTime.UtcNow.AddHours(1)));
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var logger = new Mock<ILogger<AuthService>>();
            var sut = CreateSut(new UnitOfWork(ctx), logger.Object);

            var res = await sut.VerifyOtpAsync(Request(email, ValidOtpPlain));

            Assert.True(res.Success);
            Assert.Contains(expectedSubstring, res.Message, StringComparison.OrdinalIgnoreCase);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(u => u.UserId == uid);
            Assert.Equal(UserState.PendingApproval, stored.Status);

            VerifyInformationLogContains(logger, "Dữ liệu user", Times.Once());
        }
    }

    [Fact]
    public async Task UTCID03_NoUser_ReturnsEmailNotFound()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.VerifyOtpAsync(Request("missing@local.test", ValidOtpPlain));

            Assert.False(res.Success);
            Assert.Equal("Email không tồn tại", res.Message);
        }
    }

    [Fact]
    public async Task UTCID04_ActiveUser_ReturnsNotNeedVerification()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f03-0004-0004-000000000004");
            var email = "active@local.test";
            await SeedRolesForOtpTestsAsync(ctx);
            var u = CreateUnverifiedUser(uid, email, (int)RoleUser.Vendor, HashOtp(ValidOtpPlain), DateTime.UtcNow.AddHours(1));
            u.Status = UserState.Active;
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.VerifyOtpAsync(Request(email, ValidOtpPlain));

            Assert.False(res.Success);
            Assert.Equal("Tài khoản không cần xác minh email", res.Message);
        }
    }

    public static TheoryData<string, int?, DateTime?, string> BlockedOtpScenarios =>
        new()
        {
            {
                "too-many-fail@local.test", 5, DateTime.UtcNow.AddHours(1),
                "Nhập sai OTP quá nhiều lần"
            },
            {
                "expired@local.test", 0, DateTime.UtcNow.AddHours(-1),
                "hết hạn"
            },
            {
                "null-otp@local.test", 0, DateTime.UtcNow.AddHours(1),
                "hết hạn"
            }
        };

    [Theory]
    [MemberData(nameof(BlockedOtpScenarios))]
    public async Task UTCID05_OtpGuardrails_ReturnsExpectedError(
        string email,
        int? failedCount,
        DateTime? expiresAt,
        string expectedSubstring)
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.NewGuid();
            await SeedRolesForOtpTestsAsync(ctx);
            string? otpHash = email.StartsWith("null-otp", StringComparison.Ordinal) ? null : HashOtp(ValidOtpPlain);
            ctx.Users.Add(CreateUnverifiedUser(
                uid,
                email,
                (int)RoleUser.Vendor,
                otpHash: otpHash,
                otpExpiresAt: expiresAt,
                otpFailedCount: failedCount ?? 0));
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.VerifyOtpAsync(Request(email, ValidOtpPlain));

            Assert.False(res.Success);
            Assert.Contains(expectedSubstring, res.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UTCID06_WrongOtp_IncrementsFailedCount_ReturnsAttemptsLeft()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f03-0006-0006-000000000006");
            var email = "wrong-otp@local.test";
            await SeedRolesForOtpTestsAsync(ctx);
            ctx.Users.Add(CreateUnverifiedUser(
                uid,
                email,
                (int)RoleUser.Vendor,
                otpHash: HashOtp(ValidOtpPlain),
                otpExpiresAt: DateTime.UtcNow.AddHours(1)));
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.VerifyOtpAsync(Request(email, "999999"));

            Assert.False(res.Success);
            Assert.Contains("Còn 4 lần thử", res.Message, StringComparison.Ordinal);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(u => u.UserId == uid);
            Assert.Equal(1, stored.OtpFailedCount);
        }
    }
}
