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

        var now = DateTime.UtcNow;
        var products = new List<Product>
        {
            new()
            {
                ProductId = Product1Id,
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
                ProductId = Product3Id,
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
                ProductId = Product4Id,
                SupermarketId = SupermarketBigCId,
                Name = "Rau cải xanh hữu cơ",
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
                ProductId = Product6Id,
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
                ProductId = Product7Id,
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
                ProductId = Product8Id,
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
                UnitId = UnitBottleId, // Chai
                ExpiryDate = now.AddHours(8), // Còn 8 giờ
                ManufactureDate = now.AddDays(-7),
                Quantity = 50,
                Weight = 50, // 50 chai * 1L
                OriginalUnitPrice = 32000m,
                SuggestedUnitPrice = 22000m, // Giảm mạnh vì sắp hết hạn
                FinalUnitPrice = 24000m,
                Status = "Active",
                CreatedAt = now
            },
            // Sữa tươi Vinamilk - Sắp hết hạn (ExpiringSoon - 1-2 ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                UnitId = UnitBottleId,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-5),
                Quantity = 100,
                Weight = 100,
                OriginalUnitPrice = 32000m,
                SuggestedUnitPrice = 26000m,
                FinalUnitPrice = 27000m,
                Status = "Active",
                CreatedAt = now
            },
            // Sữa chua - Còn ngắn hạn (ShortTerm - 3-7 ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                UnitId = UnitBoxId, // Hộp
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-10),
                Quantity = 200,
                Weight = 40, // 200 hộp * 200g = 40kg
                OriginalUnitPrice = 8000m,
                SuggestedUnitPrice = 6500m,
                FinalUnitPrice = 6800m,
                Status = "Active",
                CreatedAt = now
            },
            // Thịt heo ba chỉ - Hết hạn trong ngày (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product3Id,
                UnitId = UnitKgId, // Kg
                ExpiryDate = now.AddHours(12),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1, // 1 lot
                Weight = 15.5m, // 15.5 kg
                OriginalUnitPrice = 150000m, // 150k/kg
                SuggestedUnitPrice = 95000m, // Giảm mạnh
                FinalUnitPrice = 99000m,
                Status = "Active",
                CreatedAt = now
            },
            // Cá hồi - Sắp hết hạn (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product10Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Weight = 8.2m, // 8.2 kg
                OriginalUnitPrice = 450000m, // 450k/kg
                SuggestedUnitPrice = 320000m,
                FinalUnitPrice = 330000m,
                Status = "Active",
                CreatedAt = now
            },

            // === BIGC LOTS ===
            // Rau cải xanh - Hết hạn trong ngày (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddHours(6),
                ManufactureDate = now.AddDays(-1),
                Quantity = 1,
                Weight = 25m, // 25 kg
                OriginalUnitPrice = 35000m,
                SuggestedUnitPrice = 15000m, // Giảm mạnh
                FinalUnitPrice = 18000m,
                Status = "Active",
                CreatedAt = now
            },
            // Cà chua - Còn ngắn hạn (bán theo cân)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product5Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(4),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Weight = 30m, // 30 kg
                OriginalUnitPrice = 25000m,
                SuggestedUnitPrice = 18000m,
                FinalUnitPrice = 19000m,
                Status = "Active",
                CreatedAt = now
            },
            // Bánh mì - Hết hạn trong ngày
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product6Id,
                UnitId = UnitPackId, // Gói
                ExpiryDate = now.AddHours(10),
                ManufactureDate = now.AddDays(-1),
                Quantity = 80,
                Weight = 40, // 80 gói * 500g
                OriginalUnitPrice = 25000m,
                SuggestedUnitPrice = 12000m,
                FinalUnitPrice = 15000m,
                Status = "Active",
                CreatedAt = now
            },

            // === VINMART LOTS ===
            // Nước cam - Còn dài hạn (LongTerm - 8+ ngày)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                UnitId = UnitBottleId,
                ExpiryDate = now.AddDays(15),
                ManufactureDate = now.AddDays(-5),
                Quantity = 150,
                Weight = 150, // 150 chai * 1L
                OriginalUnitPrice = 45000m,
                SuggestedUnitPrice = 40000m,
                FinalUnitPrice = 42000m,
                Status = "Active",
                CreatedAt = now
            },
            // Bánh quy Oreo - Còn dài hạn (non-fresh)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product8Id,
                UnitId = UnitBoxId,
                ExpiryDate = now.AddDays(30),
                ManufactureDate = now.AddDays(-60),
                Quantity = 200,
                Weight = 53, // 200 hộp * 264g
                OriginalUnitPrice = 35000m,
                SuggestedUnitPrice = 32000m,
                FinalUnitPrice = 33000m,
                Status = "Active",
                CreatedAt = now
            },
            // Mì Hảo Hảo - Còn dài hạn (non-fresh)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product9Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(60),
                ManufactureDate = now.AddDays(-30),
                Quantity = 500,
                Weight = 37.5m, // 500 gói * 75g
                OriginalUnitPrice = 5500m,
                SuggestedUnitPrice = 5000m,
                FinalUnitPrice = 5200m,
                Status = "Active",
                CreatedAt = now
            },
            // Nước cam - Sắp hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                UnitId = UnitBottleId,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-18),
                Quantity = 30,
                Weight = 30,
                OriginalUnitPrice = 45000m,
                SuggestedUnitPrice = 28000m,
                FinalUnitPrice = 30000m,
                Status = "Active",
                CreatedAt = now
            },

            // === EXPIRED LOTS (để test filter Expired) ===
            // Sữa chua đã hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                UnitId = UnitBoxId,
                ExpiryDate = now.AddDays(-2), // Đã hết hạn 2 ngày
                ManufactureDate = now.AddDays(-17),
                Quantity = 50,
                Weight = 10,
                OriginalUnitPrice = 8000m,
                SuggestedUnitPrice = 0m,
                FinalUnitPrice = 0m,
                Status = "Expired",
                CreatedAt = now.AddDays(-10)
            },
            // Rau cải đã hết hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(-1), // Đã hết hạn 1 ngày
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Weight = 10m,
                OriginalUnitPrice = 35000m,
                SuggestedUnitPrice = 0m,
                FinalUnitPrice = 0m,
                Status = "Expired",
                CreatedAt = now.AddDays(-3)
            },

            // === ĐỒ HỘP LOTS ===
            // Cá ngừ đóng hộp - Còn dài hạn (đồ hộp thường có hạn dài)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                UnitId = UnitCanId, // Lon
                ExpiryDate = now.AddDays(180), // 6 tháng
                ManufactureDate = now.AddDays(-90),
                Quantity = 100,
                Weight = 17, // 100 lon * 170g
                OriginalUnitPrice = 28000m,
                SuggestedUnitPrice = 26000m,
                FinalUnitPrice = 27000m,
                Status = "Active",
                CreatedAt = now
            },
            // Cá ngừ - Sắp hết hạn (lô cũ)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                UnitId = UnitCanId,
                ExpiryDate = now.AddDays(3),
                ManufactureDate = now.AddDays(-360),
                Quantity = 30,
                Weight = 5.1m,
                OriginalUnitPrice = 28000m,
                SuggestedUnitPrice = 18000m,
                FinalUnitPrice = 20000m,
                Status = "Active",
                CreatedAt = now.AddDays(-30)
            },
            // Đậu đỏ hầm đường - Còn dài hạn
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                UnitId = UnitCanId,
                ExpiryDate = now.AddDays(365), // 1 năm
                ManufactureDate = now.AddDays(-30),
                Quantity = 80,
                Weight = 30.4m, // 80 lon * 380g
                OriginalUnitPrice = 22000m,
                SuggestedUnitPrice = 20000m,
                FinalUnitPrice = 21000m,
                Status = "Active",
                CreatedAt = now
            },
            // Đậu đỏ - Còn ngắn hạn (lô cũ sắp hết)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                UnitId = UnitCanId,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-360),
                Quantity = 20,
                Weight = 7.6m,
                OriginalUnitPrice = 22000m,
                SuggestedUnitPrice = 15000m,
                FinalUnitPrice = 16000m,
                Status = "Active",
                CreatedAt = now.AddDays(-60)
            }
        };

        await context.ProductLots.AddRangeAsync(productLots);
        await context.SaveChangesAsync();
    }
}