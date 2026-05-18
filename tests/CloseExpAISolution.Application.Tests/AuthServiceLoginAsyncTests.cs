using System.Linq.Expressions;
using AutoMapper;
using CloseExpAISolution.Application.DTOs.Request;
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
/// FN002 — <c>.github/instructions/login-async-test-sheet.md</c> (UTCID01–UTCID06).
/// <see cref="AuthService.LoginAsync"/>.
/// Run: <c>dotnet test --filter "FullyQualifiedName~AuthServiceLoginAsyncTests"</c>
/// </summary>
public sealed class AuthServiceLoginAsyncTests
{
    private const string ValidPassword = "Abcd1234!";

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

    private static AuthService CreateSut(IUnitOfWork uow, IConfiguration? configuration = null, ILogger<AuthService>? logger = null)
    {
        return new AuthService(
            uow,
            configuration ?? CreateJwtConfiguration(),
            Mock.Of<IEmailService>(),
            logger ?? new Mock<ILogger<AuthService>>().Object,
            Mock.Of<IMapper>(),
            Mock.Of<IMapboxService>());
    }

    private static LoginRequest Login(string email, string password) =>
        new() { Email = email, Password = password };

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

    private static User CreateBaseUser(Guid userId, string email, UserState status, short failedLoginCount = 0, DateTime? updatedAt = null)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            UserId = userId,
            Email = email,
            FullName = "Login User",
            Phone = "0901111111",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ValidPassword),
            RoleId = (int)RoleUser.Vendor,
            Status = status,
            FailedLoginCount = failedLoginCount,
            CreatedAt = now,
            UpdatedAt = updatedAt ?? now
        };
    }

    [Fact]
    public async Task UTCID01_ActiveUserCorrectPassword_ReturnsSuccess_PersistsRefreshToken()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0001-0001-0001-000000000001");
            var email = "login01@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            ctx.Users.Add(CreateBaseUser(uid, email, UserState.Active));
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx);
            var sut = CreateSut(uow);

            var res = await sut.LoginAsync(Login(email, ValidPassword));

            Assert.True(res.Success);
            Assert.NotNull(res.Data);
            Assert.False(string.IsNullOrEmpty(res.Data.AccessToken));
            Assert.False(string.IsNullOrEmpty(res.Data.RefreshToken));
            Assert.Contains("Đăng nhập thành công", res.Message, StringComparison.Ordinal);

            var tokens = await ctx.RefreshTokens.AsNoTracking().Where(t => t.UserId == uid).ToListAsync();
            Assert.NotEmpty(tokens);
        }
    }

    [Fact]
    public async Task UTCID01_AfterWrongPassword_CorrectLogin_ResetsFailedLoginCount()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0001-0002-0002-000000000002");
            var email = "login01b@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            var u = CreateBaseUser(uid, email, UserState.Active);
            u.FailedLoginCount = 2;
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            await sut.LoginAsync(Login(email, "WrongPwd1!"));

            var res = await sut.LoginAsync(Login(email, ValidPassword));
            Assert.True(res.Success);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(x => x.UserId == uid);
            Assert.Equal(0, stored.FailedLoginCount);
        }
    }

    [Fact]
    public async Task UTCID02_NoUser_ReturnsGenericInvalidCredentials()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LoginAsync(Login("ghost@local.test", ValidPassword));

            Assert.False(res.Success);
            Assert.Equal("Email hoặc mật khẩu không hợp lệ", res.Message);
            Assert.Null(res.Data);
        }
    }

    [Fact]
    public async Task UTCID03_WrongPasswordOnce_ReturnsAttemptsLeft_IncrementsCounter()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0003-0003-0003-000000000003");
            var email = "login03@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            ctx.Users.Add(CreateBaseUser(uid, email, UserState.Active));
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LoginAsync(Login(email, "WrongPwd1!"));

            Assert.False(res.Success);
            Assert.Contains("Còn 4 lần thử", res.Message, StringComparison.Ordinal);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(x => x.UserId == uid);
            Assert.Equal(1, stored.FailedLoginCount);
            Assert.Equal(UserState.Active, stored.Status);
        }
    }

    [Fact]
    public async Task UTCID04_FifthWrongPassword_LocksAccount()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("aaaaaaaa-0004-0004-0004-000000000004");
            var email = "login04@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            ctx.Users.Add(CreateBaseUser(uid, email, UserState.Active));
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            for (var i = 0; i < 4; i++)
            {
                var r = await sut.LoginAsync(Login(email, "WrongPwd1!"));
                Assert.False(r.Success);
                Assert.Contains("lần thử", r.Message, StringComparison.Ordinal);
            }

            var res = await sut.LoginAsync(Login(email, "WrongPwd1!"));

            Assert.False(res.Success);
            Assert.Contains("khóa tạm thời", res.Message, StringComparison.Ordinal);
            Assert.Contains("5", res.Message, StringComparison.Ordinal);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(x => x.UserId == uid);
            Assert.Equal(UserState.Locked, stored.Status);
            Assert.Equal(5, stored.FailedLoginCount);
        }
    }

    public static TheoryData<UserState, string> StatusBlockedSubstrings =>
        new()
        {
            { UserState.Unverified, "chưa xác minh" },
            { UserState.PendingApproval, "chờ Admin phê duyệt" },
            { UserState.Rejected, "từ chối" },
            { UserState.Banned, "vĩnh viễn" },
            { UserState.Deleted, "đã bị xóa" }
        };

    [Theory]
    [MemberData(nameof(StatusBlockedSubstrings))]
    public async Task UTCID05_AccountStatusBlocksLogin_ReturnsStatusMessage(UserState status, string expectedSubstring)
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.NewGuid();
            var email = $"status{(int)status}@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            ctx.Users.Add(CreateBaseUser(uid, email, status));
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LoginAsync(Login(email, ValidPassword));

            Assert.False(res.Success);
            Assert.Contains(expectedSubstring, res.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task UTCID05_Locked_AdminLock_ReturnsAdminMessage()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("bbbbbbbb-0005-0005-0005-000000000005");
            var email = "lockedadmin@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            var u = CreateBaseUser(uid, email, UserState.Locked, failedLoginCount: 1);
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LoginAsync(Login(email, ValidPassword));

            Assert.False(res.Success);
            Assert.Contains("Admin khóa", res.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task UTCID05_Locked_AutoLockCooldown_ReturnsRemainingMinutesMessage()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            var uid = Guid.Parse("bbbbbbbb-0005-0006-0006-000000000006");
            var email = "lockedauto@local.test";

            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            var u = CreateBaseUser(uid, email, UserState.Locked, failedLoginCount: 5);
            u.UpdatedAt = DateTime.UtcNow;
            ctx.Users.Add(u);
            await ctx.SaveChangesAsync();

            var sut = CreateSut(new UnitOfWork(ctx));
            var res = await sut.LoginAsync(Login(email, ValidPassword));

            Assert.False(res.Success);
            Assert.Contains("khóa tạm thời", res.Message, StringComparison.Ordinal);
            Assert.Contains("phút", res.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task UTCID06_CommitFails_OnSuccessPath_LogsFailedToCompleteLogin()
    {
        var activeUser = CreateBaseUser(Guid.Parse("cccccccc-0006-0006-0006-000000000001"), "ok@local.test", UserState.Active);

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(activeUser);
        userRepo.Setup(r => r.Update(It.IsAny<User>()));

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Role { RoleId = id, RoleName = "Vendor" });

        var rtRepo = new Mock<IGenericRepository<RefreshToken>>();
        rtRepo
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
            .Returns((RefreshToken t) => Task.FromResult(t));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.Repository<Role>()).Returns(roleRepo.Object);
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(rtRepo.Object);
        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("commit failed"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateSut(uow.Object, CreateJwtConfiguration(), logger.Object);

        var res = await sut.LoginAsync(Login(activeUser.Email, ValidPassword));

        Assert.False(res.Success);
        Assert.Contains("Đăng nhập thất bại", res.Message, StringComparison.Ordinal);
        VerifyLogErrorContains(logger, "Failed to complete login", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }

    [Fact]
    public async Task UTCID06_CommitFails_OnHandleFailedLogin_LogsFailedToHandleFailedLogin()
    {
        var activeUser = CreateBaseUser(Guid.Parse("cccccccc-0006-0006-0006-000000000002"), "bad@local.test", UserState.Active);

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(activeUser);
        userRepo.Setup(r => r.Update(It.IsAny<User>()));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("commit failed"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateSut(uow.Object, CreateJwtConfiguration(), logger.Object);

        var res = await sut.LoginAsync(Login(activeUser.Email, "WrongPwd1!"));

        Assert.False(res.Success);
        Assert.Contains("Đăng nhập thất bại", res.Message, StringComparison.Ordinal);
        VerifyLogErrorContains(logger, "Failed to handle failed login", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }
}
