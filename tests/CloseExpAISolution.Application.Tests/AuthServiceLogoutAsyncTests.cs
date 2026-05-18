using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.Email.Interfaces;
using CloseExpAISolution.Application.Mapbox.Interfaces;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Base;
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
/// FN005 — <c>.github/instructions/logout-async-test-sheet.md</c> (UTCID01–UTCID05).
/// <see cref="AuthService.LogoutAsync"/>.
/// Run: <c>dotnet test --filter "FullyQualifiedName~AuthServiceLogoutAsyncTests"</c>
/// </summary>
public sealed class AuthServiceLogoutAsyncTests
{
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

    private static AuthService CreateSut(IUnitOfWork uow, ILogger<AuthService>? logger = null) =>
        new AuthService(
            uow,
            new ConfigurationBuilder().Build(),
            Mock.Of<IEmailService>(),
            logger ?? new Mock<ILogger<AuthService>>().Object,
            Mock.Of<IMapper>(),
            Mock.Of<IMapboxService>());

    private static async Task SeedVendorRoleAsync(ApplicationDbContext ctx)
    {
        ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
        await ctx.SaveChangesAsync();
    }

    private static User ActiveUser(Guid uid, string email) =>
        new()
        {
            UserId = uid,
            Email = email,
            FullName = "L",
            Phone = "090",
            PasswordHash = "x",
            RoleId = (int)RoleUser.Vendor,
            Status = UserState.Active,
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static void VerifyLogErrorContains(Mock<ILogger<AuthService>> logger, string substring, Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v != null && v.ToString()!.Contains(substring, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task UTCID01_ActiveToken_SetsRevokedAt_ReturnsSuccess()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f05-0001-0001-000000000001");
            const string tok = "logout-ok-token";

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "lo01@local.test"));
            ctx.RefreshTokens.Add(new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = uid,
                Token = tok,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LogoutAsync(tok);

            Assert.True(res.Success);
            Assert.True(res.Data);
            Assert.Equal("Đăng xuất thành công", res.Message);

            var row = await ctx.RefreshTokens.AsNoTracking().SingleAsync(t => t.Token == tok);
            Assert.NotNull(row.RevokedAt);
        }
    }

    [Fact]
    public async Task UTCID02_UnknownToken_ReturnsInvalid()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LogoutAsync("no-such-logout-token");

            Assert.False(res.Success);
            Assert.Equal("Refresh token không hợp lệ", res.Message);
        }
    }

    [Fact]
    public async Task UTCID03_AlreadyRevoked_ReturnsAlreadyLoggedOut_NoExtraWrite()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f05-0003-0003-000000000003");
            const string tok = "already-out";
            var priorRevoke = DateTime.UtcNow.AddDays(-2);

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "lo03@local.test"));
            ctx.RefreshTokens.Add(new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = uid,
                Token = tok,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                RevokedAt = priorRevoke
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LogoutAsync(tok);

            Assert.True(res.Success);
            Assert.True(res.Data);
            Assert.Equal("Đã đăng xuất", res.Message);

            var row = await ctx.RefreshTokens.AsNoTracking().SingleAsync(t => t.Token == tok);
            Assert.Equal(priorRevoke, row.RevokedAt);
        }
    }

    [Fact]
    public async Task UTCID04_ExpiredButNotRevoked_StillRevokesRow()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f05-0004-0004-000000000004");
            const string tok = "expired-not-revoked";

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "lo04@local.test"));
            ctx.RefreshTokens.Add(new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UserId = uid,
                Token = tok,
                ExpiresAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                RevokedAt = null
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LogoutAsync(tok);

            Assert.True(res.Success);
            Assert.True(res.Data);
            Assert.Equal("Đăng xuất thành công", res.Message);

            var row = await ctx.RefreshTokens.AsNoTracking().SingleAsync(t => t.Token == tok);
            Assert.NotNull(row.RevokedAt);
        }
    }

    [Fact]
    public async Task UTCID05_CommitFails_LogsAndReturnsFailure()
    {
        var stored = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = "logout-commit-fail",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        var rtRepo = new Mock<IGenericRepository<RefreshToken>>();
        rtRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync(stored);
        rtRepo.Setup(r => r.Update(It.IsAny<RefreshToken>()));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(rtRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("db"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateSut(uow.Object, logger.Object);

        var res = await sut.LogoutAsync("logout-commit-fail");

        Assert.False(res.Success);
        Assert.Equal("Đăng xuất thất bại. Vui lòng thử lại", res.Message);
        VerifyLogErrorContains(logger, "Failed to logout token", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }
}
