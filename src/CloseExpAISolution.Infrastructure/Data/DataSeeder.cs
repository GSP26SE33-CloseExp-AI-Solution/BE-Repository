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
    private static readonly Guid AdminUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedAdminUserAsync(context);
        await SeedSupermarketsAsync(context);
        await SeedProductsAsync(context);
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
            UserId = AdminUserId,
            FullName = "Administrator",
            Email = "admin",
            Phone = string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            RoleId = 1,
            Status = UserState.Verified.ToString(),
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
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

        var now = DateTime.UtcNow;
        var products = new List<Product>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Sữa tươi Vinamilk 1L",
                Brand = "Vinamilk",
                Category = "Sữa & Sản phẩm từ sữa",
                Barcode = "8934673111119",
                IsFreshFood = true,
                Type = ProductType.Fresh,
                Sku = "SKU-VN-001",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Sữa tươi, vitamin D3",
                Nutrition = "Calo: 62kcal/100ml, Protein: 3.2g",
                Usage = "Uống trực tiếp hoặc dùng chế biến",
                Manufacturer = "Vinamilk",
                ResponsibleOrg = "Bộ Y tế",
                Warning = "Bảo quản lạnh 2-6°C",
                isActive = true,
                isFeatured = true,
                Tags = new[] { "sữa", "tươi", "bổ dưỡng" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketCoopMartId,
                Name = "Thịt heo ba chỉ",
                Brand = "Meat Deli",
                Category = "Thịt & Hải sản",
                Barcode = "8934673333333",
                IsFreshFood = true,
                Type = ProductType.Fresh,
                Sku = "SKU-MT-002",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Thịt heo tươi",
                Nutrition = "Protein: 21g, Chất béo: 14g/100g",
                Usage = "Chế biến các món ăn",
                Manufacturer = "Meat Deli",
                ResponsibleOrg = "Chi cục Thú y",
                Warning = "Bảo quản lạnh, dùng trong 2 ngày",
                isActive = true,
                isFeatured = false,
                Tags = new[] { "thịt", "heo", "tươi" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketBigCId,
                Name = "Rau cải xanh hữu cơ 500g",
                Brand = "Dalat Garden",
                Category = "Rau củ quả",
                Barcode = "8934673444440",
                IsFreshFood = true,
                Type = ProductType.Fresh,
                Sku = "SKU-VG-003",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Rau cải xanh hữu cơ",
                Nutrition = "Vitamin A, C, K, chất xơ",
                Usage = "Luộc, xào, nấu canh",
                Manufacturer = "Dalat Garden",
                ResponsibleOrg = "Bộ NN&PTNT",
                Warning = "Rửa sạch trước khi dùng",
                isActive = true,
                isFeatured = true,
                Tags = new[] { "rau", "hữu cơ", "đà lạt" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketBigCId,
                Name = "Bánh mì sandwich Kinh Đô",
                Brand = "Kinh Đô",
                Category = "Bánh & Đồ nướng",
                Barcode = "8934673666664",
                IsFreshFood = true,
                Type = ProductType.Standard,
                Sku = "SKU-BK-004",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Bột mì, men, đường, muối",
                Nutrition = "Calo: 265kcal/100g",
                Usage = "Ăn trực tiếp, làm sandwich",
                Manufacturer = "Kinh Đô",
                ResponsibleOrg = "Bộ Y tế",
                Warning = "Hạn sử dụng in trên bao bì",
                isActive = true,
                isFeatured = false,
                Tags = new[] { "bánh mì", "sandwich" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketVinMartId,
                Name = "Nước cam ép Tropicana 1L",
                Brand = "Tropicana",
                Category = "Đồ uống",
                Barcode = "8934673777771",
                IsFreshFood = true,
                Type = ProductType.Beverage,
                Sku = "SKU-BV-005",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Nước cam ép, vitamin C",
                Nutrition = "Vitamin C: 60mg/100ml",
                Usage = "Uống trực tiếp",
                Manufacturer = "Tropicana",
                ResponsibleOrg = "Bộ Y tế",
                Warning = "Lắc đều trước khi dùng",
                isActive = true,
                isFeatured = true,
                Tags = new[] { "nước cam", "vitamin", "bổ dưỡng" },
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                SupermarketId = SupermarketVinMartId,
                Name = "Bánh quy Oreo 264g",
                Brand = "Oreo",
                Category = "Bánh kẹo",
                Barcode = "8934673888888",
                IsFreshFood = false,
                Type = ProductType.Dry,
                Sku = "SKU-SN-006",
                CreatedBy = AdminUserId.ToString(),
                Status = ProductState.Verified.ToString(),
                Ingredients = "Bột mì, đường, cacao, dầu thực vật",
                Nutrition = "Calo: 474kcal/100g",
                Usage = "Ăn trực tiếp",
                Manufacturer = "Mondelez",
                ResponsibleOrg = "Bộ Y tế",
                Warning = "Chứa gluten, sữa",
                isActive = true,
                isFeatured = false,
                Tags = new[] { "bánh", "oreo", "snack" },
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}

