using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedAdminUserAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        await context.Database.ExecuteSqlRawAsync(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES 
            (1, 'Admin'),
            (2, 'MarketStaff'),
            (3, 'FoodVendor'),
            (4, 'DeliveryStaff'),
            (5, 'InternalStaff')
            ON CONFLICT (""RoleId"") DO NOTHING");
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Email == "admin"))
            return;

        var admin = new User
        {
            UserId = Guid.NewGuid(),
            FullName = "Administrator",
            Email = "admin",
            Phone = string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RoleId = 1,
            Status = UserState.Active.ToString(),
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}
