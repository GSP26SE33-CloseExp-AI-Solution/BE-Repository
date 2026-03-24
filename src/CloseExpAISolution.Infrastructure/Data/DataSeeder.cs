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
    private static readonly Guid SupermarketStaffUserId1 = Guid.Parse("33333333-4444-4444-4444-444444444444");
    private static readonly Guid SupermarketStaffUserId2 = Guid.Parse("55555555-6666-6666-6666-666666666666");
    private static readonly Guid SupermarketStaffUserId3 = Guid.Parse("77777777-8888-8888-8888-888888888888");
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

    // Category GUIDs
    private static readonly Guid CategoryDairyId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryMeatSeafoodId = Guid.Parse("ccca0002-0002-0002-0002-000000000002");
    private static readonly Guid CategoryVegetableId = Guid.Parse("ccca0003-0003-0003-0003-000000000003");
    private static readonly Guid CategoryDryFoodId = Guid.Parse("ccca0004-0004-0004-0004-000000000004");
    private static readonly Guid CategoryFrozenId = Guid.Parse("ccca0005-0005-0005-0005-000000000005");
    private static readonly Guid CategorySpiceId = Guid.Parse("ccca0006-0006-0006-0006-000000000006");
    /// <summary>Danh mục con (demo cây phân cấp) — con của Thực phẩm khô.</summary>
    private static readonly Guid CategorySnackSubId = Guid.Parse("ccca0007-0007-0007-0007-000000000007");

    // Product GUIDs (để có thể tạo StockLots)
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
    private static readonly Guid TimeSlotMorningId = Guid.Parse("cccc0001-0001-0001-0001-000000000001");
    private static readonly Guid TimeSlotAfternoonId = Guid.Parse("cccc0002-0002-0002-0002-000000000002");
    private static readonly Guid CollectionPointDistrict1Id = Guid.Parse("dddd0001-0001-0001-0001-000000000001");
    private static readonly Guid CollectionPointDistrict3Id = Guid.Parse("dddd0002-0002-0002-0002-000000000002");
    private static readonly Guid CustomerAddressVendor1Id = Guid.Parse("eeee0001-0001-0001-0001-000000000001");
    private static readonly Guid CustomerAddressVendor2Id = Guid.Parse("eeee0002-0002-0002-0002-000000000002");
    private static readonly Guid PackagingOrderPickupId = Guid.Parse("ffff0001-0001-0001-0001-000000000001");
    private static readonly Guid PackagingOrderHomeId = Guid.Parse("ffff0002-0002-0002-0002-000000000002");
    private static readonly Guid PackagingOrderReadyId = Guid.Parse("ffff0003-0003-0003-0003-000000000003");
    /// <summary>Sample order for vendor user 22222222-2222-2222-0000-000000000002 (PayOS / API tests).</summary>
    private static readonly Guid VendorUser3SampleOrderId = Guid.Parse("ffff0004-0004-0004-0004-000000000004");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedSupermarketsAsync(context);
        await SeedMarketStaffAsync(context);  // Liên kết SupermarketStaff với Supermarket
        await SeedUnitsAsync(context);        // Seed đơn vị tính
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
        await SeedStockLotsAsync(context);
        await SeedDeliveryTimeSlotsAsync(context);
        await SeedCollectionPointsAsync(context);
        await SeedCustomerAddressesAsync(context);
        await SeedPackagingOrdersAsync(context);
        await SeedVendorUser3SampleOrderAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        if (await context.Roles.AnyAsync())
            return;

        await context.Database.ExecuteSqlRawAsync(@"INSERT INTO ""Roles"" (""RoleId"", ""RoleName"") VALUES 
            (1, 'Admin'),
            (2, 'PackagingStaff'),
            (3, 'MarketingStaff'),
            (4, 'SupermarketStaff'),
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = StaffUserId2,
                FullName = "Tô Thị B - Nhân viên kiểm kho",
                Email = "staff.2@gmail.com",
                Phone = "0912222222",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 2,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = MarketStaffUserId2,
                FullName = "Trần Thị D - Nhân viên Marketing",
                Email = "market.2@gmail.com",
                Phone = "0913444444",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 3,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Supplier Staff (RoleId = 4)
            new()
            {
                UserId = SupermarketStaffUserId1,
                FullName = "Lê Văn E - Nhà cung cấp rau",
                Email = "supplier.1@gmail.com",
                Phone = "0914555555",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = SupermarketStaffUserId2,
                FullName = "Phạm Thị F - Nhà cung cấp thịt",
                Email = "supplier.2@gmail.com",
                Phone = "0914666666",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Supplier Staff (RoleId = 4) - No numbers
            new()
            {
                UserId = SupermarketStaffUserId3,
                FullName = "Ngô Kim Liên - Nhà cung cấp",
                Email = "supplier@gmail.com",
                Phone = "0917345678",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = DeliveryStaffUserId2,
                FullName = "Đỗ Thị H - Nhân viên giao hàng",
                Email = "delivery.2@gmail.com",
                Phone = "0915888888",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 5,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                UserId = VendorUserId2,
                FullName = "Quán cơm Cô Hòa",
                Email = "vendor.2@gmail.com",
                Phone = "0917888888",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 6,
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                Status = UserState.Active.ToString(),
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
    /// Seed MarketStaff - Liên kết SupermarketStaff users với Supermarkets
    /// </summary>
    private static async Task SeedMarketStaffAsync(ApplicationDbContext context)
    {
        if (await context.SupermarketStaffs.AnyAsync())
            return;

        var marketStaffRecords = new List<SupermarketStaff>
        {
            // SupermarketStaff 1 - Làm việc tại CoopMart
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId1,
                SupermarketId = SupermarketCoopMartId,
                Position = "Nhân viên kho",
                CreatedAt = DateTime.UtcNow
            },
            // SupermarketStaff 2 - Làm việc tại Big C
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId2,
                SupermarketId = SupermarketBigCId,
                Position = "Nhân viên quầy thịt",
                CreatedAt = DateTime.UtcNow
            },
            // SupermarketStaff 3 - Làm việc tại VinMart
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId3,
                SupermarketId = SupermarketVinMartId,
                Position = "Quản lý kho",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.SupermarketStaffs.AddRangeAsync(marketStaffRecords);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed Units - Đơn vị tính cho sản phẩm
    /// </summary>
    private static async Task SeedUnitsAsync(ApplicationDbContext context)
    {
        if (await context.UnitOfMeasures.AnyAsync())
            return;

        var units = new List<UnitOfMeasure>
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

        await context.UnitOfMeasures.AddRangeAsync(units);
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
                CategoryId = CategoryDairyId,
                SupermarketId = SupermarketCoopMartId,
                UnitId = UnitLiterId,
                Name = "Sữa tươi Vinamilk 1L",
                Barcode = "8934673111119",
                Sku = "8934673111119",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product2Id,
                CategoryId = CategoryDairyId,
                SupermarketId = SupermarketCoopMartId,
                UnitId = UnitBoxId,
                Name = "Sữa chua Vinamilk có đường",
                Barcode = "8934673222226",
                Sku = "8934673222226",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product3Id,
                CategoryId = CategoryMeatSeafoodId,
                SupermarketId = SupermarketCoopMartId,
                UnitId = UnitKgId,
                Name = "Thịt heo ba chỉ",
                Barcode = "8934673333333",
                Sku = "8934673333333",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product4Id,
                CategoryId = CategoryVegetableId,
                SupermarketId = SupermarketBigCId,
                UnitId = UnitKgId,
                Name = "Rau cải xanh hữu cơ",
                Barcode = "8934673444440",
                Sku = "8934673444440",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product5Id,
                CategoryId = CategoryVegetableId,
                SupermarketId = SupermarketBigCId,
                UnitId = UnitKgId,
                Name = "Cà chua Đà Lạt",
                Barcode = "8934673555557",
                Sku = "8934673555557",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product6Id,
                CategoryId = CategoryDryFoodId,
                SupermarketId = SupermarketBigCId,
                UnitId = UnitPackId,
                Name = "Bánh mì sandwich Kinh Đô",
                Barcode = "8934673666664",
                Sku = "8934673666664",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product7Id,
                CategoryId = CategoryDairyId,
                SupermarketId = SupermarketVinMartId,
                UnitId = UnitBottleId,
                Name = "Nước cam ép Tropicana 1L",
                Barcode = "8934673777771",
                Sku = "8934673777771",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product8Id,
                CategoryId = CategoryDryFoodId,
                SupermarketId = SupermarketVinMartId,
                UnitId = UnitBoxId,
                Name = "Bánh quy Oreo 264g",
                Barcode = "8934673888888",
                Sku = "8934673888888",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product9Id,
                CategoryId = CategoryDryFoodId,
                SupermarketId = SupermarketVinMartId,
                UnitId = UnitPackId,
                Name = "Mì Hảo Hảo tôm chua cay",
                Barcode = "8934673999995",
                Sku = "8934673999995",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product10Id,
                CategoryId = CategoryMeatSeafoodId,
                SupermarketId = SupermarketCoopMartId,
                UnitId = UnitKgId,
                Name = "Cá hồi phi lê đông lạnh",
                Barcode = "8934673101010",
                Sku = "8934673101010",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product11Id,
                CategoryId = CategoryMeatSeafoodId,
                SupermarketId = SupermarketCoopMartId,
                UnitId = UnitPackId,
                Name = "Cá ngừ đóng hộp Vissan 170g",
                Barcode = "8934673121212",
                Sku = "8934673121212",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            },
            new()
            {
                ProductId = Product12Id,
                CategoryId = CategoryDryFoodId,
                SupermarketId = SupermarketBigCId,
                UnitId = UnitPackId,
                Name = "Đậu đỏ hầm đường lon 380g",
                Barcode = "8934673131313",
                Sku = "8934673131313",
                Status = ProductState.Verified.ToString(),
                CreatedBy = AdminUserId.ToString(),
                CreatedAt = now,
                UpdatedBy = AdminUserId.ToString(),
                UpdatedAt = now,
                IsFeatured = false
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        var productDetails = new List<ProductDetail>
        {
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product1Id, Brand = "Vinamilk", Description = "Sữa tươi tiệt trùng Vinamilk 100% nguyên chất", Origin = "Việt Nam", Ingredients = "Sữa tươi nguyên chất 100%, Vitamin A, Vitamin D3", NutritionFacts = """{"calories":"120 kcal","protein":"6g","fat":"4g","carbs":"12g","calcium":"240mg"}""", UsageInstructions = "Lắc đều trước khi sử dụng. Dùng trực tiếp hoặc pha chế đồ uống.", StorageInstructions = "Bảo quản nơi khô ráo, thoáng mát. Sau khi mở nắp, bảo quản trong tủ lạnh và sử dụng trong 3 ngày.", SafetyWarning = "Không sử dụng sản phẩm hết hạn, bao bì bị phồng hoặc có mùi lạ.", Manufacturer = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", Distributor = "Vinamilk" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product2Id, Brand = "Vinamilk", Description = "Sữa chua ăn Vinamilk có đường thơm ngon bổ dưỡng", Origin = "Việt Nam", Ingredients = "Sữa tươi, đường, men sữa chua Lactobacillus bulgaricus, Streptococcus thermophilus", NutritionFacts = """{"calories":"95 kcal","protein":"4g","fat":"2.5g","carbs":"14g","sugar":"12g"}""", UsageInstructions = "Dùng trực tiếp sau khi mở nắp. Có thể dùng kèm trái cây hoặc granola.", StorageInstructions = "Bảo quản lạnh 2-6°C. Sử dụng trong ngày sau khi mở nắp.", SafetyWarning = "Không sử dụng nếu nắp hộp bị phồng hoặc hở.", Manufacturer = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", Distributor = "Vinamilk" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product3Id, Brand = "Meat Deli", Description = "Thịt heo ba chỉ tươi ngon từ trang trại", Origin = "Việt Nam", Ingredients = "Thịt heo tươi 100%", UsageInstructions = "Rửa sạch trước khi chế biến. Dùng để chiên, kho, nướng.", StorageInstructions = "Bảo quản lạnh 0-4°C, sử dụng trong 3 ngày. Hoặc đông lạnh -18°C, sử dụng trong 3 tháng.", SafetyWarning = "Nấu chín kỹ trước khi ăn.", Manufacturer = "Công ty TNHH Meat Deli", Distributor = "Meat Deli" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product4Id, Brand = "Dalat Garden", Ingredients = "Rau cải xanh hữu cơ 100%", Manufacturer = "Công ty TNHH Dalat Garden" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product5Id, Brand = "Dalat Garden", Ingredients = "Cà chua Đà Lạt 100%", Manufacturer = "Công ty TNHH Dalat Garden" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product6Id, Brand = "Kinh Đô", Ingredients = "Bột mì, đường, bơ, trứng, men nở, muối, chất bảo quản", NutritionFacts = """{"calories":"280 kcal","protein":"8g","fat":"3g","carbs":"52g","fiber":"2g"}""", Manufacturer = "Công ty Cổ phần Kinh Đô" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product7Id, Brand = "Tropicana", Ingredients = "Nước cam cô đặc 50%, nước, đường, hương cam tự nhiên, Vitamin C", NutritionFacts = """{"calories":"110 kcal","sugar":"22g","vitaminC":"120mg","carbs":"26g"}""", Manufacturer = "Công ty TNHH Tropicana" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product8Id, Brand = "Oreo", Ingredients = "Bột mì, đường, dầu thực vật, bột cacao, muối, lecithin đậu nành, vani", NutritionFacts = """{"calories":"160 kcal","fat":"7g","carbs":"25g","sugar":"14g","protein":"1g"}""", Manufacturer = "Công ty Mondelez Vietnam" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product9Id, Brand = "Acecook", Ingredients = "Bột mì, dầu ăn, muối, bột ngọt, ớt, tôm khô, hành lá khô, gia vị", NutritionFacts = """{"calories":"350 kcal","fat":"14g","carbs":"49g","protein":"7g","sodium":"1500mg"}""", Manufacturer = "Công ty Cổ phần Acecook Việt Nam" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product10Id, Brand = "Seafood King", Ingredients = "Cá hồi phi lê tươi 100%", Manufacturer = "Công ty TNHH Seafood King" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product11Id, Brand = "Vissan", Ingredients = "Cá ngừ 65%, dầu thực vật, nước, muối, bột ngọt", NutritionFacts = """{"calories":"190 kcal","protein":"26g","fat":"9g","carbs":"0g","sodium":"400mg"}""", Manufacturer = "Công ty Cổ phần Vissan" },
            new() { ProductDetailId = Guid.NewGuid(), ProductId = Product12Id, Brand = "Nếp Mới", Ingredients = "Đậu đỏ 50%, đường, nước, muối", NutritionFacts = """{"calories":"320 kcal","protein":"8g","carbs":"68g","sugar":"42g","fiber":"6g"}""", Manufacturer = "Công ty TNHH Nếp Mới" }
        };
        await context.ProductDetails.AddRangeAsync(productDetails);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new()
            {
                CategoryId = CategoryDairyId,
                Name = "Sữa & Đồ uống",
                IsFreshFood = false,
                IsActive = true,
                Description = "Sữa, đồ uống đóng chai/hộp"
            },
            new()
            {
                CategoryId = CategoryMeatSeafoodId,
                Name = "Thịt & Hải sản",
                IsFreshFood = true,
                IsActive = true,
                Description = "Nhóm thịt, cá, hải sản tươi/đông lạnh"
            },
            new()
            {
                CategoryId = CategoryVegetableId,
                Name = "Rau củ",
                IsFreshFood = true,
                IsActive = true,
                Description = "Rau củ quả tươi"
            },
            new()
            {
                CategoryId = CategoryDryFoodId,
                Name = "Thực phẩm khô",
                IsFreshFood = false,
                IsActive = true,
                Description = "Mì gói, bánh, thực phẩm đóng gói"
            },
            new()
            {
                CategoryId = CategoryFrozenId,
                Name = "Đông lạnh",
                IsFreshFood = false,
                IsActive = true,
                Description = "Thực phẩm đông lạnh, kem"
            },
            new()
            {
                CategoryId = CategorySpiceId,
                Name = "Gia vị & dầu ăn",
                IsFreshFood = false,
                IsActive = true,
                Description = "Muối, nước mắm, dầu, gia vị khô"
            },
            new()
            {
                CategoryId = CategorySnackSubId,
                ParentCatId = CategoryDryFoodId,
                Name = "Snack & đồ ăn vặt",
                IsFreshFood = false,
                IsActive = true,
                Description = "Danh mục con demo — thuộc nhóm Thực phẩm khô"
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed StockLots - Lô hàng với các mức hạn sử dụng khác nhau
    /// </summary>
    private static async Task SeedStockLotsAsync(ApplicationDbContext context)
    {
        if (await context.StockLots.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var stockLots = new List<StockLot>
        {
            // === COOPMART LOTS ===
            // === COOPMART LOTS ===
            // Sữa tươi Vinamilk - Hết hạn trong ngày (Today)
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                UnitId = UnitLiterId,
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
                UnitId = UnitLiterId,
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
                UnitId = UnitBoxId,
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
                UnitId = UnitKgId,
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
                UnitId = UnitKgId,
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
                UnitId = UnitKgId,
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
                UnitId = UnitKgId,
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
                UnitId = UnitPackId,
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
                UnitId = UnitBottleId,
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
                UnitId = UnitBoxId,
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
                UnitId = UnitPackId,
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
                UnitId = UnitBottleId,
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
                UnitId = UnitBoxId,
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
                UnitId = UnitKgId,
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
                UnitId = UnitPackId,
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
                UnitId = UnitPackId,
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
                UnitId = UnitPackId,
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
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-360),
                Quantity = 20,
                Status = "Active",
                CreatedAt = now.AddDays(-60),
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddDays(-55)
            }
        };

        await context.StockLots.AddRangeAsync(stockLots);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDeliveryTimeSlotsAsync(ApplicationDbContext context)
    {
        if (await context.DeliveryTimeSlots.AnyAsync())
            return;

        var slots = new List<DeliveryTimeSlot>
        {
            new()
            {
                DeliveryTimeSlotId = TimeSlotMorningId,
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(11, 0, 0)
            },
            new()
            {
                DeliveryTimeSlotId = TimeSlotAfternoonId,
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(17, 0, 0)
            }
        };

        await context.DeliveryTimeSlots.AddRangeAsync(slots);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCollectionPointsAsync(ApplicationDbContext context)
    {
        if (await context.CollectionPoints.AnyAsync())
            return;

        var points = new List<CollectionPoint>
        {
            new()
            {
                CollectionId = CollectionPointDistrict1Id,
                Name = "Điểm nhận hàng Quận 1",
                AddressLine = "45 Lê Lợi, Bến Nghé, Quận 1, TP.HCM"
            },
            new()
            {
                CollectionId = CollectionPointDistrict3Id,
                Name = "Điểm nhận hàng Quận 3",
                AddressLine = "72 Võ Thị Sáu, Phường Võ Thị Sáu, Quận 3, TP.HCM"
            }
        };

        await context.CollectionPoints.AddRangeAsync(points);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCustomerAddressesAsync(ApplicationDbContext context)
    {
        if (await context.CustomerAddresses.AnyAsync())
            return;

        var addresses = new List<CustomerAddress>
        {
            new()
            {
                CustomerAddressId = CustomerAddressVendor1Id,
                UserId = VendorUserId1,
                RecipientName = "Cửa hàng Tạp hóa Ngõ 5",
                Phone = "0916999999",
                AddressLine = "12 Nguyễn Trãi, Phường Bến Thành, Quận 1, TP.HCM",
                IsDefault = true
            },
            new()
            {
                CustomerAddressId = CustomerAddressVendor2Id,
                UserId = VendorUserId2,
                RecipientName = "Quán cơm Cô Hòa",
                Phone = "0917888888",
                AddressLine = "210 Điện Biên Phủ, Phường 7, Quận 3, TP.HCM",
                IsDefault = true
            }
        };

        await context.CustomerAddresses.AddRangeAsync(addresses);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPackagingOrdersAsync(ApplicationDbContext context)
    {
        if (await context.Orders.AnyAsync(o => o.OrderCode.StartsWith("PKG-")))
            return;

        var activeLots = await context.StockLots
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.ExpiryDate)
            .Take(4)
            .ToListAsync();

        if (activeLots.Count < 4)
            return;

        var now = DateTime.UtcNow;

        var pickupOrder = new Order
        {
            OrderId = PackagingOrderPickupId,
            OrderCode = "PKG-PICKUP-001",
            UserId = VendorUserId1,
            DeliveryTimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict1Id,
            AddressId = null,
            DeliveryType = "CollectionPoint",
            TotalAmount = 180000,
            DiscountAmount = 12000,
            FinalAmount = 168000,
            DeliveryFee = 0,
            Status = OrderState.Paid_Processing.ToString(),
            OrderDate = now.AddHours(-3),
            DeliveryNote = "Ưu tiên đóng gói gọn",
            CreatedAt = now.AddHours(-3),
            UpdatedAt = now.AddHours(-3)
        };

        var homeOrder = new Order
        {
            OrderId = PackagingOrderHomeId,
            OrderCode = "PKG-HOME-001",
            UserId = VendorUserId2,
            DeliveryTimeSlotId = TimeSlotAfternoonId,
            CollectionId = null,
            AddressId = CustomerAddressVendor2Id,
            DeliveryType = "HomeDelivery",
            TotalAmount = 220000,
            DiscountAmount = 15000,
            FinalAmount = 205000,
            DeliveryFee = 10000,
            Status = OrderState.Paid_Processing.ToString(),
            OrderDate = now.AddHours(-2),
            DeliveryNote = "Giao trước 16h",
            CreatedAt = now.AddHours(-2),
            UpdatedAt = now.AddHours(-2)
        };

        var readyOrder = new Order
        {
            OrderId = PackagingOrderReadyId,
            OrderCode = "PKG-READY-001",
            UserId = VendorUserId1,
            DeliveryTimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict3Id,
            AddressId = null,
            DeliveryType = "CollectionPoint",
            TotalAmount = 140000,
            DiscountAmount = 5000,
            FinalAmount = 135000,
            DeliveryFee = 0,
            Status = OrderState.Ready_To_Ship.ToString(),
            OrderDate = now.AddHours(-5),
            DeliveryNote = "Đã đóng gói",
            CreatedAt = now.AddHours(-5),
            UpdatedAt = now.AddHours(-1)
        };

        var orderItems = new List<OrderItem>
        {
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = pickupOrder.OrderId,
                LotId = activeLots[0].LotId,
                Quantity = 2,
                UnitPrice = 60000,
                TotalPrice = 120000
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = pickupOrder.OrderId,
                LotId = activeLots[1].LotId,
                Quantity = 1,
                UnitPrice = 60000,
                TotalPrice = 60000
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = homeOrder.OrderId,
                LotId = activeLots[2].LotId,
                Quantity = 2,
                UnitPrice = 90000,
                TotalPrice = 180000
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = homeOrder.OrderId,
                LotId = activeLots[3].LotId,
                Quantity = 1,
                UnitPrice = 40000,
                TotalPrice = 40000
            }
        };

        var packagingRecord = new OrderPackaging
        {
            PackagingId = Guid.NewGuid(),
            OrderId = readyOrder.OrderId,
            UserId = StaffUserId1,
            Status = "Packaged",
            PackagedAt = now.AddHours(-1)
        };

        await context.Orders.AddRangeAsync(pickupOrder, homeOrder, readyOrder);
        await context.OrderItems.AddRangeAsync(orderItems);
        await context.PackagingRecords.AddAsync(packagingRecord);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// One order owned by <see cref="VendorUserId3"/> (22222222-2222-2222-0000-000000000002), status Pending (e.g. thanh toán PayOS).
    /// </summary>
    private static async Task SeedVendorUser3SampleOrderAsync(ApplicationDbContext context)
    {
        if (await context.Orders.AnyAsync(o => o.OrderId == VendorUser3SampleOrderId))
            return;

        var lot = await context.StockLots
            .Where(x => x.Status == "Active")
            .OrderBy(x => x.ExpiryDate)
            .FirstOrDefaultAsync();

        if (lot == null)
            return;

        var now = DateTime.UtcNow;
        var order = new Order
        {
            OrderId = VendorUser3SampleOrderId,
            OrderCode = "VENDOR3-SEED-001",
            UserId = VendorUserId3,
            DeliveryTimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict1Id,
            AddressId = null,
            DeliveryType = "CollectionPoint",
            TotalAmount = 120000,
            DiscountAmount = 0,
            FinalAmount = 120000,
            DeliveryFee = 0,
            Status = OrderState.Pending.ToString(),
            OrderDate = now.AddMinutes(-30),
            DeliveryNote = "Đơn seed cho vendor@gmail.com — chờ thanh toán",
            CreatedAt = now.AddMinutes(-30),
            UpdatedAt = now.AddMinutes(-30)
        };

        var item = new OrderItem
        {
            OrderItemId = Guid.Parse("ffff0005-0005-0005-0005-000000000005"),
            OrderId = order.OrderId,
            LotId = lot.LotId,
            Quantity = 2,
            UnitPrice = 60000,
            TotalPrice = 120000
        };

        await context.Orders.AddAsync(order);
        await context.OrderItems.AddAsync(item);
        await context.SaveChangesAsync();
    }
}
