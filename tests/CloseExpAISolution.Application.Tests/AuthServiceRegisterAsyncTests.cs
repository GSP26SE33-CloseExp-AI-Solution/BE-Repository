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
/// FN001 — Columns UTCID01–UTCID08 of <c>.github/instructions/register-async-test-sheet.md</c>
/// (<see cref="AuthService.RegisterAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~AuthServiceRegisterAsyncTests"</c>
/// </summary>
public sealed class AuthServiceRegisterAsyncTests
{
    private const string ValidPassword = "Abcd1234!";
    private static readonly string ValidPhone = "0901234567";

    private static RegisterRequest BaseRequest(string email, string fullName = "Vendor User") =>
        new()
        {
            FullName = fullName,
            Email = email,
            Phone = ValidPhone,
            Password = ValidPassword,
            RegistrationType = RegistrationType.Vendor
        };

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

    private static AuthService CreateAuthService(
        IUnitOfWork uow,
        IEmailService? email = null,
        ILogger<AuthService>? logger = null)
    {
        var emailMock = email ?? Mock.Of<IEmailService>(e =>
            e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.CompletedTask);
        var loggerMock = logger ?? new Mock<ILogger<AuthService>>().Object;
        return new AuthService(
            uow,
            Mock.Of<IConfiguration>(),
            emailMock,
            loggerMock,
            Mock.Of<IMapper>(),
            Mock.Of<IMapboxService>());
    }

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
    public async Task UTCID01_NewVendorRegistration_ReturnsSuccess_AndInsertsUnverifiedUser()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            await ctx.SaveChangesAsync();

            var emailAddr = "newvendor01@local.test";
            var uow = new UnitOfWork(ctx);
            var sut = CreateAuthService(uow);

            var res = await sut.RegisterAsync(BaseRequest(emailAddr));

            Assert.True(res.Success);
            Assert.Null(res.Data);
            Assert.Contains("đăng ký thành công", res.Message, StringComparison.OrdinalIgnoreCase);

            var stored = await ctx.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == emailAddr);
            Assert.NotNull(stored);
            Assert.Equal(UserState.Unverified, stored!.Status);
            Assert.Equal((int)RoleUser.Vendor, stored.RoleId);
            Assert.False(string.IsNullOrEmpty(stored.OtpCode));
            Assert.NotNull(stored.OtpExpiresAt);
            Assert.True(BCrypt.Net.BCrypt.Verify(ValidPassword, stored.PasswordHash));
        }
    }

    [Fact]
    public async Task UTCID02_ExistingUnverifiedSameEmail_RefreshesProfileAndReturnsSuccessMessage()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            var uid = Guid.Parse("aaaaaaaa-0002-0002-0002-000000000002");
            var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPwd1!Aa");
            ctx.Users.Add(new User
            {
                UserId = uid,
                FullName = "Old Name",
                Email = "same02@local.test",
                Phone = "0900000000",
                PasswordHash = oldHash,
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Unverified,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                OtpFailedCount = 3
            });
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx);
            var sut = CreateAuthService(uow);

            var req = BaseRequest("same02@local.test", "New Name Two");
            var res = await sut.RegisterAsync(req);

            Assert.True(res.Success);
            Assert.Contains("cập nhật thông tin đăng ký", res.Message, StringComparison.OrdinalIgnoreCase);

            var stored = await ctx.Users.AsNoTracking().SingleAsync(u => u.UserId == uid);
            Assert.Equal("New Name Two", stored.FullName);
            Assert.Equal(ValidPhone, stored.Phone);
            Assert.Equal(0, stored.OtpFailedCount);
            Assert.True(BCrypt.Net.BCrypt.Verify(ValidPassword, stored.PasswordHash));
            Assert.False(string.IsNullOrEmpty(stored.OtpCode));
        }
    }

    [Fact]
    public async Task UTCID03_ExistingActiveSameEmail_ReturnsDuplicateEmailMessage()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            ctx.Users.Add(new User
            {
                UserId = Guid.Parse("aaaaaaaa-0003-0003-0003-000000000003"),
                FullName = "Active User",
                Email = "active03@local.test",
                Phone = ValidPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(ValidPassword),
                RoleId = (int)RoleUser.Vendor,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx);
            var sut = CreateAuthService(uow);

            var res = await sut.RegisterAsync(BaseRequest("active03@local.test"));

            Assert.False(res.Success);
            Assert.Contains("Email đã được đăng ký", res.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task UTCID04_MissingVendorRoleRow_ReturnsInvalidRegistrationType()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Admin, RoleName = "Admin" });
            await ctx.SaveChangesAsync();

            var uow = new UnitOfWork(ctx);
            var sut = CreateAuthService(uow);

            var res = await sut.RegisterAsync(BaseRequest("novendor04@local.test"));

            Assert.False(res.Success);
            Assert.Contains("Loại đăng ký không hợp lệ", res.Message, StringComparison.Ordinal);
            Assert.Equal(0, await ctx.Users.CountAsync());
        }
    }

    [Fact]
    public async Task UTCID05_CommitThrowsOnInsert_LogsRegisterFailure_ReturnsGenericFailure()
    {
        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo
            .Setup(r => r.GetByIdAsync((int)RoleUser.Vendor))
            .ReturnsAsync(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(Task.FromResult<User?>(null));
        userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns((User u) => Task.FromResult(u));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Role>()).Returns(roleRepo.Object);
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("simulated commit failure"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateAuthService(uow.Object, logger: logger.Object);

        var res = await sut.RegisterAsync(BaseRequest("fail05@local.test"));

        Assert.False(res.Success);
        Assert.Contains("Đăng ký thất bại", res.Message, StringComparison.Ordinal);
        VerifyLogErrorContains(logger, "Failed to register user", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }

    [Fact]
    public async Task UTCID06_CommitThrowsOnUnverifiedRefresh_LogsRefreshFailure_ReturnsGenericFailure()
    {
        var existing = new User
        {
            UserId = Guid.NewGuid(),
            Email = "unver06@local.test",
            FullName = "U",
            Phone = "0900000006",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ValidPassword),
            RoleId = (int)RoleUser.Vendor,
            Status = UserState.Unverified,
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo
            .Setup(r => r.GetByIdAsync((int)RoleUser.Vendor))
            .ReturnsAsync(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(existing);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Role>()).Returns(roleRepo.Object);
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);
        uow
            .Setup(x => x.CommitTransactionAsync())
            .ThrowsAsync(new InvalidOperationException("simulated commit failure"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = CreateAuthService(uow.Object, logger: logger.Object);

        var res = await sut.RegisterAsync(BaseRequest("unver06@local.test"));

        Assert.False(res.Success);
        Assert.Contains("Đăng ký thất bại", res.Message, StringComparison.Ordinal);
        VerifyLogErrorContains(logger, "Failed to refresh unverified registration", Times.Once());
        uow.Verify(x => x.RollbackTransactionAsync(), Times.Once());
    }

    [Fact]
    public async Task UTCID07_SendEmailThrowsAfterCommit_StillReturnsSuccess_AndLogsOtpEmailError()
    {
        var roleRepo = new Mock<IGenericRepository<Role>>();
        roleRepo
            .Setup(r => r.GetByIdAsync((int)RoleUser.Vendor))
            .ReturnsAsync(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .Returns(Task.FromResult<User?>(null));
        userRepo
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .Returns((User u) => Task.FromResult(u));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Role>()).Returns(roleRepo.Object);
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        uow.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var email = new Mock<IEmailService>();
        email
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var logger = new Mock<ILogger<AuthService>>();
        var sut = new AuthService(
            uow.Object,
            Mock.Of<IConfiguration>(),
            email.Object,
            logger.Object,
            Mock.Of<IMapper>(),
            Mock.Of<IMapboxService>());

        var res = await sut.RegisterAsync(BaseRequest("mail07@local.test"));

        Assert.True(res.Success);
        Assert.Contains("đăng ký thành công", res.Message, StringComparison.OrdinalIgnoreCase);
        VerifyLogErrorContains(logger, "Failed to send OTP email", Times.Once());
    }

    [Fact]
    public async Task UTCID08_FullNameAt100Chars_RegistersSuccessfully()
    {
        var (conn, ctx) = CreateSqliteContext();
        await using (conn)
        await using (ctx)
        {
            ctx.Roles.Add(new Role { RoleId = (int)RoleUser.Vendor, RoleName = "Vendor" });
            await ctx.SaveChangesAsync();

            var full = new string('N', 100);
            var uow = new UnitOfWork(ctx);
            var sut = CreateAuthService(uow);

            var res = await sut.RegisterAsync(BaseRequest("boundary08@local.test", full));

            Assert.True(res.Success);
            var stored = await ctx.Users.AsNoTracking().SingleAsync(u => u.Email == "boundary08@local.test");
            Assert.Equal(100, stored.FullName.Length);
        }
    }
}
