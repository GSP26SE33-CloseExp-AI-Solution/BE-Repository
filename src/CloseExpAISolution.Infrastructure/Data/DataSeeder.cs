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
    private static readonly Guid StaffUserId3 = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid MarketStaffUserId1 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid MarketStaffUserId2 = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly Guid MarketStaffUserId3 = Guid.Parse("11111111-2222-2222-2222-222222222222");
    private static readonly Guid SupplierStaffUserId1 = Guid.Parse("33333333-4444-4444-4444-444444444444");
    private static readonly Guid SupplierStaffUserId2 = Guid.Parse("55555555-6666-6666-6666-666666666666");
    private static readonly Guid SupplierStaffUserId3 = Guid.Parse("77777777-8888-8888-8888-888888888888");
    private static readonly Guid DeliveryStaffUserId1 = Guid.Parse("99999999-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DeliveryStaffUserId2 = Guid.Parse("bbbbbbbb-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DeliveryStaffUserId3 = Guid.Parse("dddddddd-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid VendorUserId1 = Guid.Parse("ffffffff-0000-0000-0000-000000000000");
    private static readonly Guid VendorUserId2 = Guid.Parse("11111111-1111-1111-0000-000000000001");
    private static readonly Guid VendorUserId3 = Guid.Parse("22222222-2222-2222-0000-000000000002");

    // Unit GUIDs
    private static readonly Guid UnitKgId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    private static readonly Guid UnitGramId = Guid.Parse("aaaa0002-0002-0002-0002-000000000002");
    private static readonly Guid UnitLiterId = Guid.Parse("aaaa0003-0003-0003-0003-000000000003");
    private static readonly Guid UnitMlId = Guid.Parse("aaaa0004-0004-0004-0004-000000000004");
    private static readonly Guid UnitBoxId = Guid.Parse("aaaa0005-0005-0005-0005-000000000005");
    private static readonly Guid UnitBottleId = Guid.Parse("aaaa0006-0006-0006-0006-000000000006");
    private static readonly Guid UnitPackId = Guid.Parse("aaaa0007-0007-0007-0007-000000000007");
    private static readonly Guid UnitPieceId = Guid.Parse("aaaa0008-0008-0008-0008-000000000008");
    private static readonly Guid UnitCanId = Guid.Parse("aaaa0009-0009-0009-0009-000000000009");
    private static readonly Guid UnitBagId = Guid.Parse("aaaa000a-000a-000a-000a-00000000000a");

    // Product GUIDs (để có thể tạo ProductLots)
    private static readonly Guid Product1Id = Guid.Parse("bbbb0001-0001-0001-0001-000000000001");
    private static readonly Guid Product2Id = Guid.Parse("bbbb0002-0002-0002-0002-000000000002");
    private static readonly Guid Product3Id = Guid.Parse("bbbb0003-0003-0003-0003-000000000003");
    private static readonly Guid Product4Id = Guid.Parse("bbbb0004-0004-0004-0004-000000000004");
    private static readonly Guid Product5Id = Guid.Parse("bbbb0005-0005-0005-0005-000000000005");
    private static readonly Guid Product6Id = Guid.Parse("bbbb0006-0006-0006-0006-000000000006");
    private static readonly Guid Product7Id = Guid.Parse("bbbb0007-0007-0007-0007-000000000007");
    private static readonly Guid Product8Id = Guid.Parse("bbbb0008-0008-0008-0008-000000000008");
    private static readonly Guid Product9Id = Guid.Parse("bbbb0009-0009-0009-0009-000000000009");
    private static readonly Guid Product10Id = Guid.Parse("bbbb000a-000a-000a-000a-00000000000a");
    private static readonly Guid Product11Id = Guid.Parse("bbbb000b-000b-000b-000b-00000000000b");
    private static readonly Guid Product12Id = Guid.Parse("bbbb000c-000c-000c-000c-00000000000c");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedSupermarketsAsync(context);
        await SeedMarketStaffAsync(context);  // Liên kết SupplierStaff với Supermarket
        await SeedUnitsAsync(context);        // Seed đơn vị tính
        await SeedProductsAsync(context);
        await SeedProductLotsAsync(context);  // Seed lô hàng
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        await context.Database.ExecuteSqlRawAsync(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES 
            (1, 'Admin'),
            (2, 'Staff'),
            (3, 'MarketingStaff'),
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

            // Staff - Internal (RoleId = 2) - No numbers
            new()
            {
                UserId = StaffUserId3,
                FullName = "Lê Văn Minh - Nhân viên kho",
                Email = "staff@gmail.com",
                Phone = "0917123456",
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

            // Market Staff (RoleId = 3) - No numbers
            new()
            {
                UserId = MarketStaffUserId3,
                FullName = "Trần Phương - Nhân viên marketing",
                Email = "market@gmail.com",
                Phone = "0917234567",
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

            // Supplier Staff (RoleId = 4) - No numbers
            new()
            {
                UserId = SupplierStaffUserId3,
                FullName = "Ngô Kim Liên - Nhà cung cấp",
                Email = "supplier@gmail.com",
                Phone = "0917345678",
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

            // Delivery Staff (RoleId = 5) - No numbers
            new()
            {
                UserId = DeliveryStaffUserId3,
                FullName = "Phan Hương - Nhân viên giao hàng",
                Email = "delivery@gmail.com",
                Phone = "0917456789",
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
            },

            // Vendor - Customer (RoleId = 6) - No numbers
            new()
            {
                UserId = VendorUserId3,
                FullName = "Cửa hàng Tạp hóa Hoa Cúc",
                Email = "vendor@gmail.com",
                Phone = "0917567890",
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

    /// <summary>
    /// Seed MarketStaff - Liên kết SupplierStaff users với Supermarkets
    /// </summary>
    private static async Task SeedMarketStaffAsync(ApplicationDbContext context)
    {
        if (await context.MarketStaff.AnyAsync())
            return;

        var marketStaffRecords = new List<MarketStaff>
        {
            // SupplierStaff 1 - Làm việc tại CoopMart
            new()
            {
                MarketStaffId = Guid.NewGuid(),
                UserId = SupplierStaffUserId1,
                SupermarketId = SupermarketCoopMartId,
                Position = "Nhân viên kho",
                CreatedAt = DateTime.UtcNow
            },
            // SupplierStaff 2 - Làm việc tại Big C
            new()
            {
                MarketStaffId = Guid.NewGuid(),
                UserId = SupplierStaffUserId2,
                SupermarketId = SupermarketBigCId,
                Position = "Nhân viên quầy thịt",
                CreatedAt = DateTime.UtcNow
            },
            // SupplierStaff 3 - Làm việc tại VinMart
            new()
            {
                MarketStaffId = Guid.NewGuid(),
                UserId = SupplierStaffUserId3,
                SupermarketId = SupermarketVinMartId,
                Position = "Quản lý kho",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.MarketStaff.AddRangeAsync(marketStaffRecords);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed Units - Đơn vị tính cho sản phẩm
    /// </summary>
    private static async Task SeedUnitsAsync(ApplicationDbContext context)
    {
        if (await context.Units.AnyAsync())
            return;

        var units = new List<Unit>
        {
            // Weight units - Đơn vị khối lượng
            new()
            {
                UnitId = UnitKgId,
                Name = "Kg",
                Type = "Weight"
            },
            new()
            {
                UnitId = UnitGramId,
                Name = "Gram",
                Type = "Weight"
            },

            // Volume units - Đơn vị thể tích
            new()
            {
                UnitId = UnitLiterId,
                Name = "Lít",
                Type = "Volume"
            },
            new()
            {
                UnitId = UnitMlId,
                Name = "ml",
                Type = "Volume"
            },

            // Count units - Đơn vị đếm
            new()
            {
                UnitId = UnitBoxId,
                Name = "Hộp",
                Type = "Count"
            },
            new()
            {
                UnitId = UnitBottleId,
                Name = "Chai",
                Type = "Count"
            },
            new()
            {
                UnitId = UnitPackId,
                Name = "Gói",
                Type = "Count"
            },
            new()
            {
                UnitId = UnitPieceId,
                Name = "Cái",
                Type = "Count"
            },
            new()
            {
                UnitId = UnitCanId,
                Name = "Lon",
                Type = "Count"
            },
            new()
            {
                UnitId = UnitBagId,
                Name = "Túi",
                Type = "Count"
            }
        };

        await context.Units.AddRangeAsync(units);
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
                ProductId = Product1Id,
                SupermarketId = SupermarketCoopMartId,
                Name = "Sữa tươi Vinamilk 1L",
                Brand = "Vinamilk",
                Category = "Sữa & Sản phẩm từ sữa",
                Barcode = "8934673111119",
                IsFreshFood = true,
                UnitId = UnitLiterId,
                QuantityType = 1, // Fixed
                Description = "Sữa tươi tiệt trùng Vinamilk 100% nguyên chất",
                Origin = "Việt Nam",
                Ingredients = "Sữa tươi nguyên chất 100%, Vitamin A, Vitamin D3",
                NutritionFactsJson = """{"calories":"120 kcal","protein":"6g","fat":"4g","carbs":"12g","calcium":"240mg"}""",
                UsageInstructions = "Lắc đều trước khi sử dụng. Dùng trực tiếp hoặc pha chế đồ uống.",
                StorageInstructions = "Bảo quản nơi khô ráo, thoáng mát. Sau khi mở nắp, bảo quản trong tủ lạnh và sử dụng trong 3 ngày.",
                SafetyWarning = "Không sử dụng sản phẩm hết hạn, bao bì bị phồng hoặc có mùi lạ.",
                Manufacturer = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
                Distributor = "Vinamilk",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            new()
            {
                ProductId = Product2Id,
                SupermarketId = SupermarketCoopMartId,
                Name = "Sữa chua Vinamilk có đường",
                Brand = "Vinamilk",
                Category = "Sữa & Sản phẩm từ sữa",
                Barcode = "8934673222226",
                IsFreshFood = true,
                UnitId = UnitBoxId,
                QuantityType = 1,
                Description = "Sữa chua ăn Vinamilk có đường thơm ngon bổ dưỡng",
                Origin = "Việt Nam",
                Ingredients = "Sữa tươi, đường, men sữa chua Lactobacillus bulgaricus, Streptococcus thermophilus",
                NutritionFactsJson = """{"calories":"95 kcal","protein":"4g","fat":"2.5g","carbs":"14g","sugar":"12g"}""",
                UsageInstructions = "Dùng trực tiếp sau khi mở nắp. Có thể dùng kèm trái cây hoặc granola.",
                StorageInstructions = "Bảo quản lạnh 2-6°C. Sử dụng trong ngày sau khi mở nắp.",
                SafetyWarning = "Không sử dụng nếu nắp hộp bị phồng hoặc hở.",
                Manufacturer = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
                Distributor = "Vinamilk",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Meat - CoopMart (Bán theo cân)
            new()
            {
                ProductId = Product3Id,
                SupermarketId = SupermarketCoopMartId,
                Name = "Thịt heo ba chỉ",
                Brand = "Meat Deli",
                Category = "Thịt & Hải sản",
                Barcode = "8934673333333",
                IsFreshFood = true,
                UnitId = UnitKgId,
                QuantityType = 2,
                DefaultPricePerKg = 150000m,
                Description = "Thịt heo ba chỉ tươi ngon từ trang trại",
                Origin = "Việt Nam",
                Ingredients = "Thịt heo tươi 100%",
                UsageInstructions = "Rửa sạch trước khi chế biến. Dùng để chiên, kho, nướng.",
                StorageInstructions = "Bảo quản lạnh 0-4°C, sử dụng trong 3 ngày. Hoặc đông lạnh -18°C, sử dụng trong 3 tháng.",
                SafetyWarning = "Nấu chín kỹ trước khi ăn.",
                Manufacturer = "Công ty TNHH Meat Deli",
                Distributor = "Meat Deli",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Vegetables - BigC (Bán theo cân)
            new()
            {
                ProductId = Product4Id,
                SupermarketId = SupermarketBigCId,
                Name = "Rau cải xanh hữu cơ",
                Brand = "Dalat Garden",
                Category = "Rau củ quả",
                Barcode = "8934673444440",
                IsFreshFood = true,
                UnitId = UnitKgId,
                QuantityType = 2,
                DefaultPricePerKg = 35000m,
                Ingredients = "Rau cải xanh hữu cơ 100%",
                Manufacturer = "Công ty TNHH Dalat Garden",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            new()
            {
                ProductId = Product5Id,
                SupermarketId = SupermarketBigCId,
                Name = "Cà chua Đà Lạt",
                Brand = "Dalat Garden",
                Category = "Rau củ quả",
                Barcode = "8934673555557",
                IsFreshFood = true,
                UnitId = UnitKgId,
                QuantityType = 2,
                DefaultPricePerKg = 25000m,
                Ingredients = "Cà chua Đà Lạt 100%",
                Manufacturer = "Công ty TNHH Dalat Garden",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Bakery - BigC
            new()
            {
                ProductId = Product6Id,
                SupermarketId = SupermarketBigCId,
                Name = "Bánh mì sandwich Kinh Đô",
                Brand = "Kinh Đô",
                Category = "Bánh & Đồ nướng",
                Barcode = "8934673666664",
                IsFreshFood = true,
                UnitId = UnitPackId,
                QuantityType = 1,
                Ingredients = "Bột mì, đường, bơ, trứng, men nở, muối, chất bảo quản",
                NutritionFactsJson = """{"calories":"280 kcal","protein":"8g","fat":"3g","carbs":"52g","fiber":"2g"}""",
                Manufacturer = "Công ty Cổ phần Kinh Đô",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Beverages - VinMart
            new()
            {
                ProductId = Product7Id,
                SupermarketId = SupermarketVinMartId,
                Name = "Nước cam ép Tropicana 1L",
                Brand = "Tropicana",
                Category = "Đồ uống",
                Barcode = "8934673777771",
                IsFreshFood = true,
                UnitId = UnitBottleId,
                QuantityType = 1,
                Ingredients = "Nước cam cô đặc 50%, nước, đường, hương cam tự nhiên, Vitamin C",
                NutritionFactsJson = """{"calories":"110 kcal","sugar":"22g","vitaminC":"120mg","carbs":"26g"}""",
                Manufacturer = "Công ty TNHH Tropicana",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Snacks - VinMart (Non-fresh)
            new()
            {
                ProductId = Product8Id,
                SupermarketId = SupermarketVinMartId,
                Name = "Bánh quy Oreo 264g",
                Brand = "Oreo",
                Category = "Bánh kẹo",
                Barcode = "8934673888888",
                IsFreshFood = false,
                UnitId = UnitBoxId,
                QuantityType = 1,
                Ingredients = "Bột mì, đường, dầu thực vật, bột cacao, muối, lecithin đậu nành, vani",
                NutritionFactsJson = """{"calories":"160 kcal","fat":"7g","carbs":"25g","sugar":"14g","protein":"1g"}""",
                Manufacturer = "Công ty Mondelez Vietnam",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Instant Noodles - VinMart (Non-fresh)
            new()
            {
                ProductId = Product9Id,
                SupermarketId = SupermarketVinMartId,
                Name = "Mì Hảo Hảo tôm chua cay",
                Brand = "Acecook",
                Category = "Thực phẩm khô",
                Barcode = "8934673999995",
                IsFreshFood = false,
                UnitId = UnitPackId,
                QuantityType = 1,
                Ingredients = "Bột mì, dầu ăn, muối, bột ngọt, ớt, tôm khô, hành lá khô, gia vị",
                NutritionFactsJson = """{"calories":"350 kcal","fat":"14g","carbs":"49g","protein":"7g","sodium":"1500mg"}""",
                Manufacturer = "Công ty Cổ phần Acecook Việt Nam",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Seafood - CoopMart (Bán theo cân)
            new()
            {
                ProductId = Product10Id,
                SupermarketId = SupermarketCoopMartId,
                Name = "Cá hồi phi lê đông lạnh",
                Brand = "Seafood King",
                Category = "Thịt & Hải sản",
                Barcode = "8934673101010",
                IsFreshFood = true,
                UnitId = UnitKgId,
                QuantityType = 2,
                DefaultPricePerKg = 450000m, // 450,000 VND/kg
                Ingredients = "Cá hồi phi lê tươi 100%",
                Manufacturer = "Công ty TNHH Seafood King",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Đồ hộp - CoopMart
            new()
            {
                ProductId = Product11Id,
                SupermarketId = SupermarketCoopMartId,
                Name = "Cá ngừ đóng hộp Vissan 170g",
                Brand = "Vissan",
                Category = "Đồ hộp",
                Barcode = "8934673121212",
                IsFreshFood = false,
                UnitId = UnitPackId,
                QuantityType = 1,
                Ingredients = "Cá ngừ 65%, dầu thực vật, nước, muối, bột ngọt",
                NutritionFactsJson = """{"calories":"190 kcal","protein":"26g","fat":"9g","carbs":"0g","sodium":"400mg"}""",
                Manufacturer = "Công ty Cổ phần Vissan",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            },
            // Đồ hộp - BigC
            new()
            {
                ProductId = Product12Id,
                SupermarketId = SupermarketBigCId,
                Name = "Đậu đỏ hầm đường lon 380g",
                Brand = "Nếp Mới",
                Category = "Đồ hộp",
                Barcode = "8934673131313",
                IsFreshFood = false,
                UnitId = UnitPackId,
                QuantityType = 1,
                Ingredients = "Đậu đỏ 50%, đường, nước, muối",
                NutritionFactsJson = """{"calories":"320 kcal","protein":"8g","carbs":"68g","sugar":"42g","fiber":"6g"}""",
                Manufacturer = "Công ty TNHH Nếp Mới",
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isActive = true,
                isFeatured = false,
                Status = ProductState.Verified.ToString()
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed ProductLots - Lô hàng với các mức hạn sử dụng khác nhau
    /// </summary>
    private static async Task SeedProductLotsAsync(ApplicationDbContext context)
    {
        if (await context.ProductLots.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var productLots = new List<ProductLot>
        {
            // === COOPMART LOTS ===
            // Sữa tươi Vinamilk - Hết hạn trong ngày (Today)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                ExpiryDate = now.AddHours(8),
                ManufactureDate = now.AddDays(-7),
                Quantity = 50,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-2)
            },
            // Sữa tươi Vinamilk - Sắp hết hạn (ExpiringSoon - 1-2 ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-5),
                Quantity = 100,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-1)
            },
            // Sữa chua - Còn ngắn hạn (ShortTerm - 3-7 ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-10),
                Quantity = 200,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-3)
            },
            // Thịt heo ba chỉ - Hết hạn trong ngày (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product3Id,
                ExpiryDate = now.AddHours(12),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-4)
            },
            // Cá hồi - Sắp hết hạn (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product10Id,
                ExpiryDate = now.AddDays(1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-5)
            },

            // === BIGC LOTS ===
            // Rau cải xanh - Hết hạn trong ngày (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                ExpiryDate = now.AddHours(6),
                ManufactureDate = now.AddDays(-1),
                Quantity = 1,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-6)
            },
            // Cà chua - Còn ngắn hạn (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product5Id,
                ExpiryDate = now.AddDays(4),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-7)
            },
            // Bánh mì - Hết hạn trong ngày
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product6Id,
                ExpiryDate = now.AddHours(10),
                ManufactureDate = now.AddDays(-1),
                Quantity = 80,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-8)
            },

            // === VINMART LOTS ===
            // Nước cam - Còn dài hạn (LongTerm - 8+ ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                ExpiryDate = now.AddDays(15),
                ManufactureDate = now.AddDays(-5),
                Quantity = 150,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-9)
            },
            // Bánh quy Oreo - Còn dài hạn (non-fresh)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product8Id,
                ExpiryDate = now.AddDays(30),
                ManufactureDate = now.AddDays(-60),
                Quantity = 200,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-10)
            },
            // Mì Hảo Hảo - Còn dài hạn (non-fresh)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product9Id,
                ExpiryDate = now.AddDays(60),
                ManufactureDate = now.AddDays(-30),
                Quantity = 500,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-11)
            },
            // Nước cam - Sắp hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-18),
                Quantity = 30,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-12)
            },

            // === EXPIRED LOTS (để test filter Expired) ===
            // Sữa chua đã hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                ExpiryDate = now.AddDays(-2),
                ManufactureDate = now.AddDays(-17),
                Quantity = 50,
                Status = "Expired",
                CreatedAt = now.AddDays(-10)
            },
            // Rau cải đã hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                ExpiryDate = now.AddDays(-1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Status = "Expired",
                CreatedAt = now.AddDays(-3)
            },

            // === ĐỒ HỘP LOTS ===
            // Cá ngừ đóng hộp - Còn dài hạn (đồ hộp thường có hạn dài)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                ExpiryDate = now.AddDays(180),
                ManufactureDate = now.AddDays(-90),
                Quantity = 100,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddDays(-1)
            },
            // Cá ngừ - Sắp hết hạn (lô cũ)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                ExpiryDate = now.AddDays(3),
                ManufactureDate = now.AddDays(-360),
                Quantity = 30,
                Status = "Active",
                CreatedAt = now.AddDays(-30),
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddDays(-25)
            },
            // Đậu đỏ hầm đường - Còn dài hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                ExpiryDate = now.AddDays(365),
                ManufactureDate = now.AddDays(-30),
                Quantity = 80,
                Status = "Active",
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddDays(-2)
            },
            // Đậu đỏ - Còn ngắn hạn (lô cũ sắp hết)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-360),
                Quantity = 20,
                Status = "Active",
                CreatedAt = now.AddDays(-60),
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddDays(-55)
            }
        };

        await context.ProductLots.AddRangeAsync(productLots);
        await context.SaveChangesAsync();
    }
}