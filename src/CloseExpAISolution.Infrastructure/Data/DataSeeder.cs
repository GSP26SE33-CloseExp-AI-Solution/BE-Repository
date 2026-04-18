using CloseExpAISolution.Domain;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Data;

public static class DataSeeder
{
    private const string OrderCancelWindowMinutesAfterPaidValue = "30";
    private const string OrderAutoConfirmDaysAfterDeliveredValue = "3";

    private static readonly Guid SupermarketCoopMartId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SupermarketBigCId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SupermarketVinMartId = Guid.Parse("33333333-3333-3333-3333-333333333333");

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

    private static readonly Guid CategoryDairyId = Guid.Parse("ccca0001-0001-0001-0001-000000000001");
    private static readonly Guid CategoryMeatSeafoodId = Guid.Parse("ccca0002-0002-0002-0002-000000000002");
    private static readonly Guid CategoryVegetableId = Guid.Parse("ccca0003-0003-0003-0003-000000000003");
    private static readonly Guid CategoryDryFoodId = Guid.Parse("ccca0004-0004-0004-0004-000000000004");
    private static readonly Guid CategoryFrozenId = Guid.Parse("ccca0005-0005-0005-0005-000000000005");
    private static readonly Guid CategorySpiceId = Guid.Parse("ccca0006-0006-0006-0006-000000000006");
    private static readonly Guid CategorySnackSubId = Guid.Parse("ccca0007-0007-0007-0007-000000000007");
    private static readonly Guid CategoryFruitFreshId = Guid.Parse("ccca0008-0008-0008-0008-000000000008");
    private static readonly Guid CategoryBreakfastCerealId = Guid.Parse("ccca0009-0009-0009-0009-000000000009");
    private static readonly Guid CategoryCannedGoodsId = Guid.Parse("ccca000a-000a-000a-000a-00000000000a");
    private static readonly Guid CategoryVegetarianId = Guid.Parse("ccca000b-000b-000b-000b-00000000000b");
    private static readonly Guid CategoryTofuEggId = Guid.Parse("ccca000c-000c-000c-000c-00000000000c");
    private static readonly Guid CategoryLeafyGreensId = Guid.Parse("ccca000d-000d-000d-000d-00000000000d");
    private static readonly Guid CategoryBiscuitCandyId = Guid.Parse("ccca000e-000e-000e-000e-00000000000e");
    private static readonly Guid CategoryInstantFoodId = Guid.Parse("ccca000f-000f-000f-000f-00000000000f");

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
    private static readonly Guid VendorUser3SampleOrderId = Guid.Parse("ffff0004-0004-0004-0004-000000000004");

    private static readonly Guid SeedTxnPickupId = Guid.Parse("fffa1111-1111-1111-1111-111111111111");
    private static readonly Guid SeedTxnHomeId = Guid.Parse("fffa1111-2222-2222-2222-222222222222");
    private static readonly Guid SeedTxnReadyId = Guid.Parse("fffa1111-3333-3333-3333-333333333333");
    private static readonly Guid SeedRefundPendingId = Guid.Parse("fffa2222-1111-1111-1111-111111111111");
    private static readonly Guid SeedRefundApprovedId = Guid.Parse("fffa2222-2222-2222-2222-222222222222");
    private static readonly Guid SeedRefundRejectedId = Guid.Parse("fffa2222-3333-3333-3333-333333333333");
    private static readonly Guid SeedRefundCompletedId = Guid.Parse("fffa2222-4444-4444-4444-444444444444");

    private static readonly Guid CoverageExpiredLot1Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
    private static readonly Guid CoverageExpiredLot2Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222");
    private static readonly Guid CoverageTodayLot1Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333");
    private static readonly Guid CoverageTodayLot2Id = Guid.Parse("aaaa4444-4444-4444-4444-444444444444");
    private static readonly Guid CoverageExpiringSoonLot1Id = Guid.Parse("aaaa5555-5555-5555-5555-555555555555");
    private static readonly Guid CoverageExpiringSoonLot2Id = Guid.Parse("aaaa6666-6666-6666-6666-666666666666");
    private static readonly Guid CoverageShortTermLot1Id = Guid.Parse("aaaa7777-7777-7777-7777-777777777777");
    private static readonly Guid CoverageShortTermLot2Id = Guid.Parse("aaaa8888-8888-8888-8888-888888888888");
    private static readonly Guid CoverageLongTermLot1Id = Guid.Parse("aaaa9999-9999-9999-9999-999999999999");
    private static readonly Guid CoverageLongTermLot2Id = Guid.Parse("aaaa0000-9999-9999-9999-999999999999");

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedRolesAsync(context);
        await SeedUsersAsync(context);
        await SeedSystemConfigsAsync(context);
        await SeedSupermarketsAsync(context);
        await SeedMarketStaffAsync(context);
        await SeedUnitsAsync(context);
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
        await SeedStockLotsAsync(context);
        await SeedExpiryStatusCoverageStockLotsAsync(context);
        await SeedDeliveryTimeSlotsAsync(context);
        await SeedCollectionPointsAsync(context);
        await SeedCustomerAddressesAsync(context);
        await SeedPackagingOrdersAsync(context);
        await SeedVendorUser3SampleOrderAsync(context);
        await SeedSampleTransactionsAndRefundsAsync(context);
    }

    private static async Task SeedSystemConfigsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        await EnsurePositiveIntSystemConfigAsync(
            context,
            SystemConfigKeys.OrderCancelWindowMinutesAfterPaid,
            OrderCancelWindowMinutesAfterPaidValue,
            now);

        await EnsurePositiveIntSystemConfigAsync(
            context,
            SystemConfigKeys.OrderAutoConfirmDaysAfterDelivered,
            OrderAutoConfirmDaysAfterDeliveredValue,
            now);
    }

    private static async Task EnsurePositiveIntSystemConfigAsync(
        ApplicationDbContext context,
        string configKey,
        string defaultValue,
        DateTime now)
    {
        var existing = await context.SystemConfigs
            .FirstOrDefaultAsync(x => x.ConfigKey == configKey);

        if (existing == null)
        {
            await context.SystemConfigs.AddAsync(new SystemConfig
            {
                ConfigKey = configKey,
                ConfigValue = defaultValue,
                UpdatedAt = now
            });
            await context.SaveChangesAsync();
            return;
        }

        if (!int.TryParse(existing.ConfigValue, out var value) || value <= 0)
        {
            existing.ConfigValue = defaultValue;
            existing.UpdatedAt = now;
            context.SystemConfigs.Update(existing);
            await context.SaveChangesAsync();
        }
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
            new()
            {
                UserId = AdminUserId,
                FullName = "Quản trị viên hệ thống",
                Email = "admin@gmail.com",
                Phone = "0912345678",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 1,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = StaffUserId1,
                FullName = "Vũ Văn A - Nhân viên kho",
                Email = "staff.1@gmail.com",
                Phone = "0912111111",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 2,
                Status = UserState.Active,
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
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = StaffUserId3,
                FullName = "Lê Văn Minh - Nhân viên kho",
                Email = "staff@gmail.com",
                Phone = "0917123456",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 2,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = MarketStaffUserId1,
                FullName = "Nguyễn Văn C - Nhân viên Marketing",
                Email = "market.1@gmail.com",
                Phone = "0913333333",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 3,
                Status = UserState.Active,
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
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = MarketStaffUserId3,
                FullName = "Trần Phương - Nhân viên marketing",
                Email = "market@gmail.com",
                Phone = "0917234567",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 3,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = SupermarketStaffUserId1,
                FullName = "Lê Văn E - Nhà cung cấp rau",
                Email = "supplier.1@gmail.com",
                Phone = "0914555555",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Active,
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
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = SupermarketStaffUserId3,
                FullName = "Ngô Kim Liên - Nhà cung cấp",
                Email = "supplier@gmail.com",
                Phone = "0917345678",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 4,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = DeliveryStaffUserId1,
                FullName = "Hoàng Văn G - Nhân viên giao hàng",
                Email = "delivery.1@gmail.com",
                Phone = "0915777777",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 5,
                Status = UserState.Active,
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
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = DeliveryStaffUserId3,
                FullName = "Phan Hương - Nhân viên giao hàng",
                Email = "delivery@gmail.com",
                Phone = "0917456789",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 5,
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = VendorUserId1,
                FullName = "Cửa hàng Tạp hóa Ngõ 5",
                Email = "vendor.1@gmail.com",
                Phone = "0916999999",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 6,
                Status = UserState.Active,
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
                Status = UserState.Active,
                FailedLoginCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            new()
            {
                UserId = VendorUserId3,
                FullName = "Cửa hàng Tạp hóa Hoa Cúc",
                Email = "vendor@gmail.com",
                Phone = "0917567890",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                RoleId = 6,
                Status = UserState.Active,
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
                Status = SupermarketState.Active,
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
                Status = SupermarketState.Active,
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
                Status = SupermarketState.Active,
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Supermarkets.AddRangeAsync(supermarkets);
        await context.SaveChangesAsync();
    }

    private static async Task SeedMarketStaffAsync(ApplicationDbContext context)
    {
        if (await context.SupermarketStaffs.AnyAsync())
            return;

        var marketStaffRecords = new List<SupermarketStaff>
        {
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId1,
                SupermarketId = SupermarketCoopMartId,
                Position = "Nhân viên kho",
                CreatedAt = DateTime.UtcNow,
                IsManager = true
            },
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId2,
                SupermarketId = SupermarketBigCId,
                Position = "Nhân viên quầy thịt",
                CreatedAt = DateTime.UtcNow,
                IsManager = true
            },
            new()
            {
                SupermarketStaffId = Guid.NewGuid(),
                UserId = SupermarketStaffUserId3,
                SupermarketId = SupermarketVinMartId,
                Position = "Quản lý kho",
                CreatedAt = DateTime.UtcNow,
                IsManager = true
            }
        };

        await context.SupermarketStaffs.AddRangeAsync(marketStaffRecords);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitsAsync(ApplicationDbContext context)
    {
        if (await context.UnitOfMeasures.AnyAsync())
            return;

        var units = new List<UnitOfMeasure>
        {
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
                Name = "Sữa tươi Vinamilk 1L",
                Barcode = "8934673111119",
                Sku = "8934673111119",
                Status = ProductState.Verified,
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
                Name = "Sữa chua Vinamilk có đường",
                Barcode = "8934673222226",
                Sku = "8934673222226",
                Status = ProductState.Verified,
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
                Name = "Thịt heo ba chỉ",
                Barcode = "8934673333333",
                Sku = "8934673333333",
                Status = ProductState.Verified,
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
                Name = "Rau cải xanh hữu cơ",
                Barcode = "8934673444440",
                Sku = "8934673444440",
                Status = ProductState.Verified,
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
                Name = "Cà chua Đà Lạt",
                Barcode = "8934673555557",
                Sku = "8934673555557",
                Status = ProductState.Verified,
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
                Name = "Bánh mì sandwich Kinh Đô",
                Barcode = "8934673666664",
                Sku = "8934673666664",
                Status = ProductState.Verified,
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
                Name = "Nước cam ép Tropicana 1L",
                Barcode = "8934673777771",
                Sku = "8934673777771",
                Status = ProductState.Verified,
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
                Name = "Bánh quy Oreo 264g",
                Barcode = "8934673888888",
                Sku = "8934673888888",
                Status = ProductState.Verified,
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
                Name = "Mì Hảo Hảo tôm chua cay",
                Barcode = "8934673999995",
                Sku = "8934673999995",
                Status = ProductState.Verified,
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
                Name = "Cá hồi phi lê đông lạnh",
                Barcode = "8934673101010",
                Sku = "8934673101010",
                Status = ProductState.Verified,
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
                Name = "Cá ngừ đóng hộp Vissan 170g",
                Barcode = "8934673121212",
                Sku = "8934673121212",
                Status = ProductState.Verified,
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
                Name = "Đậu đỏ hầm đường lon 380g",
                Barcode = "8934673131313",
                Sku = "8934673131313",
                Status = ProductState.Verified,
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
                Description = "Danh mục con — self-ref ParentCatId → Thực phẩm khô"
            },
            new()
            {
                CategoryId = CategoryFruitFreshId,
                Name = "Trái cây tươi",
                IsFreshFood = true,
                IsActive = true,
                Description = "Trái cây theo mùa"
            },
            new()
            {
                CategoryId = CategoryBreakfastCerealId,
                Name = "Ngũ cốc & điểm tâm",
                IsFreshFood = false,
                IsActive = true,
                Description = "Ngũ cốc ăn sáng, yến mạch, granola"
            },
            new()
            {
                CategoryId = CategoryCannedGoodsId,
                Name = "Đồ hộp & đóng lon",
                IsFreshFood = false,
                IsActive = true,
                Description = "Thịt cá hộp, rau củ đóng lon"
            },
            new()
            {
                CategoryId = CategoryVegetarianId,
                Name = "Thực phẩm chay",
                IsFreshFood = false,
                IsActive = true,
                Description = "Đồ chay, thịt thực vật"
            },
            new()
            {
                CategoryId = CategoryTofuEggId,
                Name = "Đậu phụ & trứng",
                IsFreshFood = true,
                IsActive = true,
                Description = "Đậu phụ, đậu hũ, trứng gia cầm"
            },
            new()
            {
                CategoryId = CategoryLeafyGreensId,
                ParentCatId = CategoryVegetableId,
                Name = "Rau lá xanh",
                IsFreshFood = true,
                IsActive = true,
                Description = "Cải, rau muống, xà lách — con của Rau củ (ParentCatId self-ref)"
            },
            new()
            {
                CategoryId = CategoryBiscuitCandyId,
                ParentCatId = CategorySnackSubId,
                Name = "Bánh quy & kẹo",
                IsFreshFood = false,
                IsActive = true,
                Description = "Cấp 3: Thực phẩm khô → Snack → Bánh quy & kẹo"
            },
            new()
            {
                CategoryId = CategoryInstantFoodId,
                Name = "Đồ ăn liền",
                IsFreshFood = false,
                IsActive = true,
                Description = "Xôi, cháo gói, meal kit"
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedStockLotsAsync(ApplicationDbContext context)
    {
        if (await context.StockLots.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var stockLots = new List<StockLot>
        {
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                UnitId = UnitLiterId,
                ExpiryDate = now.AddHours(8),
                ManufactureDate = now.AddDays(-7),
                Quantity = 50,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-2)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product1Id,
                UnitId = UnitLiterId,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-5),
                Quantity = 100,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-1)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                UnitId = UnitBoxId,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-10),
                Quantity = 200,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-3)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product3Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddHours(12),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-4)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product10Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-5)
            },

            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddHours(6),
                ManufactureDate = now.AddDays(-1),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-6)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product5Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(4),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-7)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product6Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddHours(10),
                ManufactureDate = now.AddDays(-1),
                Quantity = 80,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-8)
            },

            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                UnitId = UnitBottleId,
                ExpiryDate = now.AddDays(15),
                ManufactureDate = now.AddDays(-5),
                Quantity = 150,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-9)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product8Id,
                UnitId = UnitBoxId,
                ExpiryDate = now.AddDays(30),
                ManufactureDate = now.AddDays(-60),
                Quantity = 200,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-10)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product9Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(60),
                ManufactureDate = now.AddDays(-30),
                Quantity = 500,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-11)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product7Id,
                UnitId = UnitBottleId,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-18),
                Quantity = 30,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-12)
            },

            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product2Id,
                UnitId = UnitBoxId,
                ExpiryDate = now.AddDays(-2),
                ManufactureDate = now.AddDays(-17),
                Quantity = 50,
                Status = ProductState.Expired,
                CreatedAt = now.AddDays(-10)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product4Id,
                UnitId = UnitKgId,
                ExpiryDate = now.AddDays(-1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Status = ProductState.Expired,
                CreatedAt = now.AddDays(-3)
            },

            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(180),
                ManufactureDate = now.AddDays(-90),
                Quantity = 100,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddDays(-1)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product11Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(3),
                ManufactureDate = now.AddDays(-360),
                Quantity = 30,
                Status = ProductState.Published,
                CreatedAt = now.AddDays(-30),
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddDays(-25)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(365),
                ManufactureDate = now.AddDays(-30),
                Quantity = 80,
                Status = ProductState.Published,
                CreatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddDays(-2)
            },
            new()
            {
                LotId = Guid.NewGuid(),
                ProductId = Product12Id,
                UnitId = UnitPackId,
                ExpiryDate = now.AddDays(5),
                ManufactureDate = now.AddDays(-360),
                Quantity = 20,
                Status = ProductState.Published,
                CreatedAt = now.AddDays(-60),
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddDays(-55)
            }
        };

        foreach (var lot in stockLots)
        {
            lot.UnitId = ResolveUnitIdByProduct(lot.ProductId);
            lot.UpdatedAt = lot.CreatedAt == default ? now : lot.CreatedAt;

            var originalPrice = ResolveOriginalPriceByProduct(lot.ProductId);
            var suggestedPrice = CalculateSuggestedPrice(originalPrice, lot.ExpiryDate, now);

            lot.OriginalUnitPrice = originalPrice;
            lot.SuggestedUnitPrice = suggestedPrice;
        }

        await context.StockLots.AddRangeAsync(stockLots);
        await context.SaveChangesAsync();
    }

    private static async Task SeedExpiryStatusCoverageStockLotsAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        var coverageLotIds = new[]
        {
            CoverageExpiredLot1Id,
            CoverageExpiredLot2Id,
            CoverageTodayLot1Id,
            CoverageTodayLot2Id,
            CoverageExpiringSoonLot1Id,
            CoverageExpiringSoonLot2Id,
            CoverageShortTermLot1Id,
            CoverageShortTermLot2Id,
            CoverageLongTermLot1Id,
            CoverageLongTermLot2Id
        };

        var existingIds = await context.StockLots
            .Where(x => coverageLotIds.Contains(x.LotId))
            .Select(x => x.LotId)
            .ToListAsync();

        var coverageLots = new List<StockLot>
        {
            new()
            {
                LotId = CoverageExpiredLot1Id,
                ProductId = Product2Id,
                ExpiryDate = now.AddDays(-1),
                ManufactureDate = now.AddDays(-12),
                Quantity = 30,
                Status = ProductState.Expired,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now
            },
            new()
            {
                LotId = CoverageExpiredLot2Id,
                ProductId = Product4Id,
                ExpiryDate = now.AddDays(-2),
                ManufactureDate = now.AddDays(-5),
                Quantity = 1,
                Status = ProductState.Expired,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now
            },

            new()
            {
                LotId = CoverageTodayLot1Id,
                ProductId = Product1Id,
                ExpiryDate = now.AddHours(4),
                ManufactureDate = now.AddDays(-6),
                Quantity = 40,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddMinutes(-30)
            },
            new()
            {
                LotId = CoverageTodayLot2Id,
                ProductId = Product6Id,
                ExpiryDate = now.AddHours(10),
                ManufactureDate = now.AddDays(-1),
                Quantity = 35,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddMinutes(-45)
            },

            new()
            {
                LotId = CoverageExpiringSoonLot1Id,
                ProductId = Product3Id,
                ExpiryDate = now.AddDays(1),
                ManufactureDate = now.AddDays(-3),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-1)
            },
            new()
            {
                LotId = CoverageExpiringSoonLot2Id,
                ProductId = Product7Id,
                ExpiryDate = now.AddDays(2),
                ManufactureDate = now.AddDays(-15),
                Quantity = 25,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-2)
            },

            new()
            {
                LotId = CoverageShortTermLot1Id,
                ProductId = Product5Id,
                ExpiryDate = now.AddDays(4),
                ManufactureDate = now.AddDays(-2),
                Quantity = 1,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-3)
            },
            new()
            {
                LotId = CoverageShortTermLot2Id,
                ProductId = Product12Id,
                ExpiryDate = now.AddDays(6),
                ManufactureDate = now.AddDays(-120),
                Quantity = 18,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId2.ToString(),
                PublishedAt = now.AddHours(-4)
            },

            new()
            {
                LotId = CoverageLongTermLot1Id,
                ProductId = Product8Id,
                ExpiryDate = now.AddDays(15),
                ManufactureDate = now.AddDays(-45),
                Quantity = 45,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId3.ToString(),
                PublishedAt = now.AddHours(-5)
            },
            new()
            {
                LotId = CoverageLongTermLot2Id,
                ProductId = Product11Id,
                ExpiryDate = now.AddDays(45),
                ManufactureDate = now.AddDays(-30),
                Quantity = 60,
                Status = ProductState.Published,
                CreatedAt = now,
                UpdatedAt = now,
                PublishedBy = MarketStaffUserId1.ToString(),
                PublishedAt = now.AddHours(-6)
            }
        };

        var missingLots = coverageLots
            .Where(x => !existingIds.Contains(x.LotId))
            .ToList();

        if (!missingLots.Any())
            return;

        foreach (var lot in missingLots)
        {
            lot.UnitId = ResolveUnitIdByProduct(lot.ProductId);

            var originalPrice = ResolveOriginalPriceByProduct(lot.ProductId);
            var suggestedPrice = CalculateSuggestedPrice(originalPrice, lot.ExpiryDate, now);
            lot.OriginalUnitPrice = originalPrice;
            lot.SuggestedUnitPrice = suggestedPrice;

            if (lot.Status == ProductState.Published && lot.ExpiryDate > now)
            {
                lot.Status = ProductState.Published;
            }
        }

        await context.StockLots.AddRangeAsync(missingLots);
        await context.SaveChangesAsync();
    }

    private static Guid ResolveUnitIdByProduct(Guid productId)
    {
        if (productId == Product1Id) return UnitLiterId;
        if (productId == Product2Id) return UnitBoxId;
        if (productId == Product3Id) return UnitKgId;
        if (productId == Product4Id) return UnitKgId;
        if (productId == Product5Id) return UnitKgId;
        if (productId == Product6Id) return UnitPackId;
        if (productId == Product7Id) return UnitBottleId;
        if (productId == Product8Id) return UnitBoxId;
        if (productId == Product9Id) return UnitPackId;
        if (productId == Product10Id) return UnitKgId;
        if (productId == Product11Id) return UnitPackId;
        if (productId == Product12Id) return UnitCanId;

        return UnitPieceId;
    }

    private static decimal ResolveOriginalPriceByProduct(Guid productId)
    {
        if (productId == Product1Id) return 36000m;
        if (productId == Product2Id) return 32000m;
        if (productId == Product3Id) return 165000m;
        if (productId == Product4Id) return 38000m;
        if (productId == Product5Id) return 42000m;
        if (productId == Product6Id) return 28000m;
        if (productId == Product7Id) return 45000m;
        if (productId == Product8Id) return 25000m;
        if (productId == Product9Id) return 5000m;
        if (productId == Product10Id) return 295000m;
        if (productId == Product11Id) return 42000m;
        if (productId == Product12Id) return 22000m;

        return 10000m;
    }

    private static decimal CalculateSuggestedPrice(decimal originalPrice, DateTime expiryDate, DateTime now)
    {
        var hoursRemaining = (expiryDate - now).TotalHours;

        var discountRate = hoursRemaining switch
        {
            <= 0 => 0.70m,
            <= 24 => 0.50m,
            <= 72 => 0.35m,
            <= 168 => 0.20m,
            _ => 0.10m
        };

        return Math.Round(originalPrice * (1 - discountRate), 0, MidpointRounding.AwayFromZero);
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
                AddressLine = "45 Lê Lợi, Bến Nghé, Quận 1, TP.HCM",
                Latitude = 10.7756m,
                Longitude = 106.7004m
            },
            new()
            {
                CollectionId = CollectionPointDistrict3Id,
                Name = "Điểm nhận hàng Quận 3",
                AddressLine = "72 Võ Thị Sáu, Phường Võ Thị Sáu, Quận 3, TP.HCM",
                Latitude = 10.7834m,
                Longitude = 106.6880m
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
                Latitude = 10.7721m,
                Longitude = 106.6980m,
                IsDefault = true
            },
            new()
            {
                CustomerAddressId = CustomerAddressVendor2Id,
                UserId = VendorUserId2,
                RecipientName = "Quán cơm Cô Hòa",
                Phone = "0917888888",
                AddressLine = "210 Điện Biên Phủ, Phường 7, Quận 3, TP.HCM",
                Latitude = 10.7863m,
                Longitude = 106.6922m,
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
            .Where(x => x.Status == ProductState.Published)
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
            TimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict1Id,
            AddressId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 180000,
            DiscountAmount = 12000,
            FinalAmount = 168000,
            DeliveryFee = 0,
            Status = OrderState.Paid,
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
            TimeSlotId = TimeSlotAfternoonId,
            CollectionId = null,
            AddressId = CustomerAddressVendor2Id,
            DeliveryType = DeliveryMethod.Delivery,
            TotalAmount = 220000,
            DiscountAmount = 15000,
            FinalAmount = 205000,
            DeliveryFee = 10000,
            Status = OrderState.Paid,
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
            TimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict3Id,
            AddressId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 140000,
            DiscountAmount = 5000,
            FinalAmount = 135000,
            DeliveryFee = 0,
            Status = OrderState.ReadyToShip,
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
                TotalPrice = 120000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-2)
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = pickupOrder.OrderId,
                LotId = activeLots[1].LotId,
                Quantity = 1,
                UnitPrice = 60000,
                TotalPrice = 60000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-2)
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = homeOrder.OrderId,
                LotId = activeLots[2].LotId,
                Quantity = 2,
                UnitPrice = 90000,
                TotalPrice = 180000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-1)
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = homeOrder.OrderId,
                LotId = activeLots[3].LotId,
                Quantity = 1,
                UnitPrice = 40000,
                TotalPrice = 40000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-1)
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = readyOrder.OrderId,
                LotId = activeLots[0].LotId,
                Quantity = 1,
                UnitPrice = 80000,
                TotalPrice = 80000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-1)
            },
            new()
            {
                OrderItemId = Guid.NewGuid(),
                OrderId = readyOrder.OrderId,
                LotId = activeLots[1].LotId,
                Quantity = 1,
                UnitPrice = 60000,
                TotalPrice = 60000,
                PackagingStatus = PackagingState.Completed,
                PackagedAt = now.AddHours(-1)
            }
        };

        // Extra seed dataset for delivery-group generation tests.
        // Use fixed plans (non-random) so test data is stable across runs.
        var generatedOrders = new List<Order>();
        var generatedItems = new List<OrderItem>();
        var generatedPackagingRecords = new List<OrderPackaging>();

        var isPickupPlan = new[]
        {
            true, false, true, false, true, false,
            true, false, true, false, true, false
        };

        var useAfternoonSlotPlan = new[]
        {
            false, true, false, true, false, true,
            false, true, false, true, false, true
        };

        var itemPlans = new (int LotIndex, short Quantity, decimal UnitPrice)[][]
        {
            new[] { (0, (short)1, 45000m), (1, (short)2, 50000m), (2, (short)1, 55000m), (3, (short)1, 60000m) },
            new[] { (2, (short)2, 55000m) },
            new[] { (3, (short)1, 60000m), (0, (short)1, 45000m), (2, (short)1, 55000m), (1, (short)1, 50000m), (0, (short)1, 45000m) },
            new[] { (1, (short)2, 50000m), (3, (short)1, 60000m) },
            new[] { (0, (short)3, 45000m), (2, (short)1, 55000m), (1, (short)1, 50000m) },
            new[] { (2, (short)1, 55000m), (1, (short)1, 50000m), (3, (short)1, 60000m), (0, (short)2, 45000m) },
            new[] { (3, (short)2, 60000m), (0, (short)1, 45000m) },
            new[] { (1, (short)1, 50000m) },
            new[] { (2, (short)2, 55000m), (0, (short)1, 45000m), (1, (short)1, 50000m), (3, (short)1, 60000m), (1, (short)1, 50000m) },
            new[] { (3, (short)1, 60000m), (2, (short)1, 55000m) },
            new[] { (0, (short)2, 45000m), (2, (short)1, 55000m), (3, (short)1, 60000m), (1, (short)1, 50000m) },
            new[] { (1, (short)1, 50000m), (2, (short)1, 55000m), (3, (short)1, 60000m), (0, (short)1, 45000m), (2, (short)1, 55000m) }
        };

        for (var i = 0; i < itemPlans.Length; i++)
        {
            var isPickup = isPickupPlan[i];
            var orderId = Guid.NewGuid();
            var orderItemsForCurrentOrder = new List<OrderItem>();
            decimal orderTotal = 0;

            foreach (var itemPlan in itemPlans[i])
            {
                var lot = activeLots[itemPlan.LotIndex];
                var itemTotal = itemPlan.Quantity * itemPlan.UnitPrice;

                orderItemsForCurrentOrder.Add(new OrderItem
                {
                    OrderItemId = Guid.NewGuid(),
                    OrderId = orderId,
                    LotId = lot.LotId,
                    Quantity = itemPlan.Quantity,
                    UnitPrice = itemPlan.UnitPrice,
                    TotalPrice = itemTotal,
                    PackagingStatus = PackagingState.Completed,
                    PackagedAt = now.AddMinutes(-(10 + i * 3))
                });

                orderTotal += itemTotal;
            }

            var order = new Order
            {
                OrderId = orderId,
                OrderCode = $"PKG-GRP-{(i + 1).ToString("D3")}",
                UserId = isPickup ? VendorUserId1 : VendorUserId2,
                TimeSlotId = useAfternoonSlotPlan[i] ? TimeSlotAfternoonId : TimeSlotMorningId,
                CollectionId = isPickup ? CollectionPointDistrict1Id : null,
                AddressId = isPickup ? null : CustomerAddressVendor2Id,
                DeliveryType = isPickup ? DeliveryMethod.Pickup : DeliveryMethod.Delivery,
                TotalAmount = orderTotal,
                DiscountAmount = 0,
                FinalAmount = orderTotal,
                DeliveryFee = isPickup ? 0 : 10000,
                Status = OrderState.Paid,
                OrderDate = now.AddMinutes(-(20 + i * 4)),
                DeliveryNote = "Seed data for delivery-group generation test",
                CreatedAt = now.AddMinutes(-(20 + i * 4)),
                UpdatedAt = now.AddMinutes(-(20 + i * 4))
            };

            generatedOrders.Add(order);
            generatedItems.AddRange(orderItemsForCurrentOrder);
            generatedPackagingRecords.Add(new OrderPackaging
            {
                PackagingId = Guid.NewGuid(),
                OrderId = orderId,
                UserId = i % 2 == 0 ? StaffUserId1 : StaffUserId2,
                Status = PackagingState.Completed,
                PackagedAt = now.AddMinutes(-(10 + i * 3))
            });
        }

        var packagingRecord = new OrderPackaging
        {
            PackagingId = Guid.NewGuid(),
            OrderId = readyOrder.OrderId,
            UserId = StaffUserId1,
            Status = PackagingState.Completed,
            PackagedAt = now.AddHours(-1)
        };

        await context.Orders.AddRangeAsync(pickupOrder, homeOrder, readyOrder);
        await context.Orders.AddRangeAsync(generatedOrders);
        await context.OrderItems.AddRangeAsync(orderItems);
        await context.OrderItems.AddRangeAsync(generatedItems);
        await context.PackagingRecords.AddAsync(packagingRecord);
        await context.PackagingRecords.AddRangeAsync(generatedPackagingRecords);
        await context.SaveChangesAsync();
    }

    private static async Task SeedVendorUser3SampleOrderAsync(ApplicationDbContext context)
    {
        if (await context.Orders.AnyAsync(o => o.OrderId == VendorUser3SampleOrderId))
            return;

        var lot = await context.StockLots
            .Where(x => x.Status == ProductState.Published)
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
            TimeSlotId = TimeSlotMorningId,
            CollectionId = CollectionPointDistrict1Id,
            AddressId = null,
            DeliveryType = DeliveryMethod.Pickup,
            TotalAmount = 120000,
            DiscountAmount = 0,
            FinalAmount = 120000,
            DeliveryFee = 0,
            Status = OrderState.Pending,
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

    private static async Task SeedSampleTransactionsAndRefundsAsync(ApplicationDbContext context)
    {
        if (await context.Transactions.AnyAsync(t => t.TransactionId == SeedTxnPickupId))
            return;

        if (!await context.Orders.AnyAsync(o => o.OrderId == PackagingOrderPickupId))
            return;

        var now = DateTime.UtcNow;
        var paid = PaymentState.Paid;

        var transactions = new List<Transaction>
        {
            new()
            {
                TransactionId = SeedTxnPickupId,
                OrderId = PackagingOrderPickupId,
                Amount = 168000,
                PaymentMethod = "PayOS",
                PaymentStatus = paid,
                CreatedAt = now.AddHours(-4),
                UpdatedAt = now.AddHours(-4),
                PayOSOrderCode = 9_001_000_000_000_001,
                PayOSPaymentLinkId = "seed-link-pickup",
                CheckoutUrl = "https://pay.payos.vn/web/seed-pickup"
            },
            new()
            {
                TransactionId = SeedTxnHomeId,
                OrderId = PackagingOrderHomeId,
                Amount = 205000,
                PaymentMethod = "PayOS",
                PaymentStatus = paid,
                CreatedAt = now.AddHours(-3),
                UpdatedAt = now.AddHours(-3),
                PayOSOrderCode = 9_001_000_000_000_002,
                PayOSPaymentLinkId = "seed-link-home",
                CheckoutUrl = "https://pay.payos.vn/web/seed-home"
            },
            new()
            {
                TransactionId = SeedTxnReadyId,
                OrderId = PackagingOrderReadyId,
                Amount = 135000,
                PaymentMethod = "PayOS",
                PaymentStatus = paid,
                CreatedAt = now.AddHours(-6),
                UpdatedAt = now.AddHours(-6),
                PayOSOrderCode = 9_001_000_000_000_003,
                PayOSPaymentLinkId = "seed-link-ready",
                CheckoutUrl = "https://pay.payos.vn/web/seed-ready"
            }
        };

        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();

        var refunds = new List<Refund>
        {
            new()
            {
                RefundId = SeedRefundPendingId,
                OrderId = PackagingOrderPickupId,
                TransactionId = SeedTxnPickupId,
                Amount = 50_000,
                Reason = "[Seed] Khách yêu cầu hoàn một phần — chờ duyệt",
                Status = RefundState.Pending,
                CreatedAt = now.AddHours(-2)
            },
            new()
            {
                RefundId = SeedRefundApprovedId,
                OrderId = PackagingOrderHomeId,
                TransactionId = SeedTxnHomeId,
                Amount = 25_000,
                Reason = "[Seed] Đã duyệt hoàn tiền giao hàng trễ",
                Status = RefundState.Approved,
                ProcessedBy = AdminUserId.ToString(),
                ProcessedAt = now.AddHours(-1),
                CreatedAt = now.AddHours(-2)
            },
            new()
            {
                RefundId = SeedRefundRejectedId,
                OrderId = PackagingOrderHomeId,
                TransactionId = SeedTxnHomeId,
                Amount = 99_000,
                Reason = "[Seed] Yêu cầu hoàn không hợp lệ (demo Rejected)",
                Status = RefundState.Rejected,
                ProcessedBy = AdminUserId.ToString(),
                ProcessedAt = now.AddMinutes(-90),
                CreatedAt = now.AddHours(-2)
            },
            new()
            {
                RefundId = SeedRefundCompletedId,
                OrderId = PackagingOrderReadyId,
                TransactionId = SeedTxnReadyId,
                Amount = 15_000,
                Reason = "[Seed] Hoàn tiền sau khi đóng gói — đã chuyển khoản",
                Status = RefundState.Completed,
                ProcessedBy = AdminUserId.ToString(),
                ProcessedAt = now.AddHours(-1),
                CreatedAt = now.AddHours(-3)
            }
        };

        await context.Refunds.AddRangeAsync(refunds);
        await context.SaveChangesAsync();
    }
}




