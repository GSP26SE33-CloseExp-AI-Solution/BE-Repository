using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Data;

public static class DataSeeder
{
    // Fixed GUIDs for seeding (for foreign key references)
    private static readonly Guid SupermarketCoopMartId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SupermarketBigCId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupermarketVinMartId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    // User GUIDs
    private static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StaffUserId1 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid StaffUserId2 = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid MarketStaffUserId1 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid MarketStaffUserId2 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid SupplierStaffUserId1 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid SupplierStaffUserId2 = Guid.Parse("11111111-2222-2222-2222-222222222222");
    private static readonly Guid DeliveryStaffUserId1 = Guid.Parse("33333333-4444-4444-4444-444444444444");
    private static readonly Guid DeliveryStaffUserId2 = Guid.Parse("55555555-6666-6666-6666-666666666666");
    private static readonly Guid VendorUserId1 = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid VendorUserId2 = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedSupermarketsAsync(context);
        await SeedProductsAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        await context.Database.ExecuteSqlRawAsync(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES 
            (1, 'Admin'),
            (2, 'Staff'),
            (3, 'MarketStaff'),
            (4, 'SupplierStaff'),
            (5, 'DeliveryStaff'),
            (6, 'Vendor')
            ON CONFLICT (""RoleId"") DO NOTHING");
    }

    private static async Task SeedUsersAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        var users = new List<User>
        {
            // Admin (RoleId = 1)
            new()
            {
                UserId = AdminUserId,
                FullName = "Quản trị viên hệ thống",
                Email = "admin@gmail.com",
                Phone = "0912345678",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 1,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },

            // Staff - Internal (RoleId = 2)
            new()
            {
                UserId = StaffUserId1,
                FullName = "Vũ Văn A - Nhân viên kho",
                Email = "staff.1@gmail.com",
                Phone = "0912111111",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 2,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },
            new()
            {
                UserId = StaffUserId2,
                FullName = "Tô Thị B - Nhân viên kiểm kho",
                Email = "staff.2@gmail.com",
                Phone = "0912222222",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 2,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },

            // Market Staff (RoleId = 3)
            new()
            {
                UserId = MarketStaffUserId1,
                FullName = "Nguyễn Văn C - Nhân viên Marketing",
                Email = "market.1@gmail.com",
                Phone = "0913333333",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 3,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },
            new()
            {
                UserId = MarketStaffUserId2,
                FullName = "Trần Thị D - Nhân viên Marketing",
                Email = "market.2@gmail.com",
                Phone = "0913444444",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 3,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },

            // Supplier Staff (RoleId = 4)
            new()
            {
                UserId = SupplierStaffUserId1,
                FullName = "Lê Văn E - Nhà cung cấp rau",
                Email = "supplier.1@gmail.com",
                Phone = "0914555555",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },
            new()
            {
                UserId = SupplierStaffUserId2,
                FullName = "Phạm Thị F - Nhà cung cấp thịt",
                Email = "supplier.2@gmail.com",
                Phone = "0914666666",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },

            // Delivery Staff (RoleId = 5)
            new()
            {
                UserId = DeliveryStaffUserId1,
                FullName = "Hoàng Văn G - Nhân viên giao hàng",
                Email = "delivery.1@gmail.com",
                Phone = "0915777777",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 5,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },
            new()
            {
                UserId = DeliveryStaffUserId2,
                FullName = "Đỗ Thị H - Nhân viên giao hàng",
                Email = "delivery.2@gmail.com",
                Phone = "0915888888",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 5,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },

            // Vendor - Customer (RoleId = 6)
            new()
            {
                UserId = VendorUserId1,
                FullName = "Cửa hàng Tạp hóa Ngõ 5",
                Email = "vendor.1@gmail.com",
                Phone = "0916999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 6,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            },
            new()
            {
                UserId = VendorUserId2,
                FullName = "Quán cơm Cô Hòa",
                Email = "vendor.2@gmail.com",
                Phone = "0917888888",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 6,
                Status = UserState.Verified.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedSupermarketsAsync(ApplicationDbContext context)
    {
        if (await context.Supermarkets.AnyAsync())
            return;

        var supermarkets = new List<Supermarket>
        {
            new()
            {
                SupermarketId = SupermarketCoopMartId,
                Name = "CoopMart Quận 1",
                Address = "168 Nguyễn Đình Chiểu, Phường 6, Quận 3, TP.HCM",
                Latitude = 10.7769m,
                Longitude = 106.6869m,
                ContactPhone = "028-3930-5678",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                SupermarketId = SupermarketBigCId,
                Name = "Big C Thảo Điền",
                Address = "222 Xa lộ Hà Nội, Phường Thảo Điền, TP. Thủ Đức, TP.HCM",
                Latitude = 10.8025m,
                Longitude = 106.7385m,
                ContactPhone = "028-3744-1234",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                SupermarketId = SupermarketVinMartId,
                Name = "VinMart Landmark 81",
                Address = "772 Điện Biên Phủ, Phường 22, Quận Bình Thạnh, TP.HCM",
                Latitude = 10.7950m,
                Longitude = 106.7220m,
                ContactPhone = "028-3636-8888",
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Supermarkets.AddRangeAsync(supermarkets);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            // Dairy Products - CoopMart
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Sữa tươi Vinamilk 1L",
                Brand = "Vinamilk",
                Category = "Sữa & Sản phẩm từ sữa",
                Barcode = "8934673111119",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Sữa chua Vinamilk có đường",
                Brand = "Vinamilk",
                Category = "Sữa & Sản phẩm từ sữa",
                Barcode = "8934673222226",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Meat - CoopMart
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Thịt heo ba chỉ",
                Brand = "Meat Deli",
                Category = "Thịt & Hải sản",
                Barcode = "8934673333333",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Vegetables - BigC
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketBigCId,
                Name = "Rau cải xanh hữu cơ 500g",
                Brand = "Dalat Garden",
                Category = "Rau củ quả",
                Barcode = "8934673444440",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketBigCId,
                Name = "Cà chua Đà Lạt 1kg",
                Brand = "Dalat Garden",
                Category = "Rau củ quả",
                Barcode = "8934673555557",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Bakery - BigC
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketBigCId,
                Name = "Bánh mì sandwich Kinh Đô",
                Brand = "Kinh Đô",
                Category = "Bánh & Đồ nướng",
                Barcode = "8934673666664",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Beverages - VinMart
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketVinMartId,
                Name = "Nước cam ép Tropicana 1L",
                Brand = "Tropicana",
                Category = "Đồ uống",
                Barcode = "8934673777771",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Snacks - VinMart (Non-fresh)
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketVinMartId,
                Name = "Bánh quy Oreo 264g",
                Brand = "Oreo",
                Category = "Bánh kẹo",
                Barcode = "8934673888888",
                IsFreshFood = false,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Instant Noodles - VinMart (Non-fresh)
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketVinMartId,
                Name = "Mì Hảo Hảo tôm chua cay",
                Brand = "Acecook",
                Category = "Thực phẩm khô",
                Barcode = "8934673999995",
                IsFreshFood = false,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            },
            // Seafood - CoopMart
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Cá hồi phi lê đông lạnh 500g",
                Brand = "Seafood King",
                Category = "Thịt & Hải sản",
                Barcode = "8934673101010",
                IsFreshFood = true,
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                Status = ProductState.Verified.ToString()
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}

