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
/// FN004 — <c>.github/instructions/refresh-token-async-test-sheet.md</c> (UTCID01–UTCID06).
/// <see cref="AuthService.RefreshTokenAsync"/>.
/// Run: <c>dotnet test --filter "FullyQualifiedName~AuthServiceRefreshTokenAsyncTests"</c>
/// </summary>
public sealed class AuthServiceRefreshTokenAsyncTests
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

    private static IConfiguration CreateJwtConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', 32),
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryInMinutes"] = "60"
            })
            .Build();

    private static AuthService CreateSut(IUnitOfWork uow, IConfiguration? configuration = null, ILogger<AuthService>? logger = null) =>
        new AuthService(
            uow,
            configuration ?? CreateJwtConfiguration(),
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
            FullName = "R",
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
    public async Task UTCID01_ValidToken_RotatesRefresh_ReturnsAuthResponse()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f04-0001-0001-000000000001");
            const string oldRt = "old-refresh-token-01";

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "rt01@local.test"));
            ctx.RefreshTokens.Add(new RefreshToken
            {
                RefreshTokenId = Guid.Parse("bbbbbbbb-0f04-0001-0001-000000000001"),
                UserId = uid,
                Token = oldRt,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                RevokedAt = null
            });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.RefreshTokenAsync(oldRt);

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.False(string.IsNullOrEmpty(res.Data!.AccessToken));
            Assert.False(string.IsNullOrEmpty(res.Data.RefreshToken));
            Assert.NotEqual(oldRt, res.Data.RefreshToken);
            Assert.Contains("Làm mới token thành công", res.Message, StringComparison.Ordinal);

            var rows = await ctx.RefreshTokens.AsNoTracking().Where(t => t.UserId == uid).OrderBy(t => t.CreatedAt).ToListAsync();
            Assert.Equal(2, rows.Count);
            Assert.NotNull(rows.Single(t => t.Token == oldRt).RevokedAt);
            Assert.NotNull(rows.Single(t => t.Token == oldRt).ReplacedByToken);
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
            var res = await sut.RefreshTokenAsync("no-such-token");

            Assert.False(res.Success);
            Assert.Equal("Refresh token không hợp lệ", res.Message);
        }
    }

    [Fact]
    public async Task UTCID03_ExpiredToken_ReturnsExpiredMessage()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f04-0003-0003-000000000003");
            const string tok = "expired-rt";

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "rt03@local.test"));
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
            var res = await sut.RefreshTokenAsync(tok);

            Assert.False(res.Success);
            Assert.Equal("Refresh token đã hết hạn", res.Message);
        }
    }

    [Fact]
    public async Task UTCID04_RevokedToken_Reuses_RevokesOtherActiveAndReturnsSessionRevoked()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f04-0004-0004-000000000004");
            const string revokedInput = "already-revoked-rt";
            const string otherActive = "still-active-rt";

            await SeedVendorRoleAsync(ctx);
            ctx.Users.Add(ActiveUser(uid, "rt04@local.test"));
            ctx.RefreshTokens.AddRange(
                new RefreshToken
                {
                    RefreshTokenId = Guid.NewGuid(),
                    UserId = uid,
                    Token = revokedInput,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    RevokedAt = DateTime.UtcNow.AddDays(-1)
                },
                new RefreshToken
                {
                    RefreshTokenId = Guid.NewGuid(),
                    UserId = uid,
                    Token = otherActive,
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    RevokedAt = null
                });
            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.RefreshTokenAsync(revokedInput);

            Assert.False(res.Success);
            Assert.Contains("thu hồi", res.Message, StringComparison.Ordinal);
            Assert.NotNull(res.Errors);
            Assert.Contains("SESSION_REVOKED", res.Errors);

            var other = await ctx.RefreshTokens.AsNoTracking().SingleAsync(t => t.Token == otherActive);
            Assert.NotNull(other.RevokedAt);
        }
    }

    [Fact]
    public async Task UTCID05_NonActiveUser_ReturnsAccountInactive()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0f04-0005-0005-000000000005");
            const string tok = "ok-rt-but-user-locked";

            await SeedVendorRoleAsync(ctx);
            var u = ActiveUser(uid, "rt05@local.test");
            u.Status = UserState.Locked;
            ctx.Users.Add(u);
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
            var res = await sut.RefreshTokenAsync(tok);

            Assert.False(res.Success);
            Assert.Equal("Tài khoản không còn hoạt động", res.Message);
        }
    }

    [Fact]
    public async Task UTCID05_UserMissing_ReturnsUserNotFound()
    {
        var uid = Guid.Parse("aaaaaaaa-0f04-0005-0005-000000000099");
        var stored = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = uid,
            Token = "orphan-rt",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        var rtRepo = new Mock<IGenericRepository<RefreshToken>>();
        rtRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync(stored);

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((User?)null);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(rtRepo.Object);
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);

        var sut = CreateSut(uow.Object);
        var res = await sut.RefreshTokenAsync("orphan-rt");

        Assert.False(res.Success);
        Assert.Equal("Người dùng không tồn tại", res.Message);
    }

    [Fact]
    public async Task UTCID06_CommitFails_LogsAndReturnsGenericFailure()
    {
        var uid = Guid.Parse("aaaaaaaa-0f04-0006-0006-000000000006");
        var stored = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            UserId = uid,
            Token = "rotate-me",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        var user = ActiveUser(uid, "rt06@local.test");

        var rtRepo = new Mock<IGenericRepository<RefreshToken>>();
        rtRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<RefreshToken, bool>>>()))
            .ReturnsAsync(stored);
        rtRepo.Setup(r => r.Update(It.IsAny<RefreshToken>()));
        rtRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns((RefreshToken t) => Task.FromResult(t));

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(user);

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Role { RoleId = id, RoleName = "Vendor" });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(rtRepo.Object);
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.Repository<Role>()).Returns(roleRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("db"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateSut(uow.Object, CreateJwtConfiguration(), logger.Object);

        var res = await sut.RefreshTokenAsync("rotate-me");

        Assert.False(res.Success);
        Assert.Contains("Làm mới token thất bại", res.Message, StringComparison.Ordinal);
        VerifyLogErrorContains(logger, "Failed to refresh token", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }
}
