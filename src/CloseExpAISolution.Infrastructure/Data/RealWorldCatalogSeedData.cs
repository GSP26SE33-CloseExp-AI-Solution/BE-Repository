using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Infrastructure.Data;

internal static class RealWorldCatalogSeedData
{
    internal static readonly Guid SupermarketLotteId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    internal static readonly Guid SupermarketBachHoaXanhId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    internal static readonly Guid SupermarketWinMartId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    internal static readonly Guid SupermarketMegaMarketId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    internal static readonly Guid SupplierStaffUserLotteId = Guid.Parse("bbbb0003-0003-0003-0003-000000000003");
    internal static readonly Guid SupplierStaffUserBachHoaXanhId = Guid.Parse("bbbb0004-0004-0004-0004-000000000004");
    internal static readonly Guid SupplierStaffUserWinMartId = Guid.Parse("bbbb0005-0005-0005-0005-000000000005");
    internal static readonly Guid SupplierStaffUserMegaMarketId = Guid.Parse("bbbb0006-0006-0006-0006-000000000006");

    internal sealed record SupermarketEntry(
        Guid SupermarketId,
        string Name,
        string Address,
        decimal Latitude,
        decimal Longitude,
        string ContactPhone);

    internal sealed record ProductEntry(
        Guid ProductId,
        Guid SupermarketId,
        Guid CategoryId,
        Guid UnitId,
        string Name,
        string Barcode,
        string Brand,
        string? Description,
        decimal OriginalPriceVnd,
        int ExpiryDays,
        int Quantity);

    internal sealed record StockLotEntry(
        Guid LotId,
        Guid ProductId,
        Guid UnitId,
        int ExpiryDays,
        decimal Quantity,
        ProductState Status,
        decimal? OriginalUnitPriceVnd = null);

    private static readonly Guid UnitKgId = Guid.Parse("aaaa0001-0001-0001-0001-000000000001");
    private static readonly Guid UnitGramId = Guid.Parse("aaaa0002-0002-0002-0002-000000000002");
    private static readonly Guid UnitBoxId = Guid.Parse("aaaa0005-0005-0005-0005-000000000005");
    private static readonly Guid UnitBottleId = Guid.Parse("aaaa0006-0006-0006-0006-000000000006");
    private static readonly Guid UnitPackId = Guid.Parse("aaaa0007-0007-0007-0007-000000000007");
    private static readonly Guid UnitPieceId = Guid.Parse("aaaa0008-0008-0008-0008-000000000008");
    private static readonly Guid UnitCanId = Guid.Parse("aaaa0009-0009-0009-0009-000000000009");
    private static readonly Guid UnitBagId = Guid.Parse("aaaa000a-000a-000a-000a-00000000000a");
    private static readonly Guid UnitThungId = Guid.Parse("aaaa000b-000b-000b-000b-00000000000b");
    private static readonly Guid CategoryTofuEggId = Guid.Parse("ccca000c-000c-000c-000c-00000000000c");

    internal static readonly SupermarketEntry[] Supermarkets =
    [
        new(
            SupermarketLotteId,
            "Lotte Mart Gò Vấp",
            "242 Nguyễn Văn Đậu, Phường 11, Quận Bình Thạnh, TP.HCM",
            10.8142m,
            106.6885m,
            "028-3848-7777"),
        new(
            SupermarketBachHoaXanhId,
            "Bách Hóa Xanh Nguyễn Thị Thập",
            "469 Nguyễn Thị Thập, Phường Tân Phú, Quận 7, TP.HCM",
            10.7415m,
            106.7028m,
            "1900-1888-79"),
        new(
            SupermarketWinMartId,
            "WinMart+ Crescent Mall",
            "101 Tôn Dật Tiên, Phường Tân Phú, Quận 7, TP.HCM",
            10.7295m,
            106.7189m,
            "028-5413-6888"),
        new(
            SupermarketMegaMarketId,
            "MM Mega Market An Phú",
            "161/11 Xa lộ Hà Nội, Phường Thảo Điền, TP. Thủ Đức, TP.HCM",
            10.8038m,
            106.7482m,
            "028-3744-9000"),
    ];

    internal sealed record SupplierStaffEntry(
        Guid UserId,
        Guid SupermarketId,
        string Email,
        string FullName,
        string Phone,
        string Position);

    internal static readonly SupplierStaffEntry[] SupplierStaffAccounts =
    [
        new(
            SupplierStaffUserLotteId,
            SupermarketLotteId,
            "supplier.3@gmail.com",
            "Trần Văn Lotte - Nhà cung cấp",
            "0914777777",
            "Quản lý kho Lotte Mart"),
        new(
            SupplierStaffUserBachHoaXanhId,
            SupermarketBachHoaXanhId,
            "supplier.4@gmail.com",
            "Lê Thị BHX - Nhà cung cấp",
            "0914888888",
            "Quản lý kho Bách Hóa Xanh"),
        new(
            SupplierStaffUserWinMartId,
            SupermarketWinMartId,
            "supplier.5@gmail.com",
            "Phạm Văn WinMart - Nhà cung cấp",
            "0914999999",
            "Quản lý kho WinMart+"),
        new(
            SupplierStaffUserMegaMarketId,
            SupermarketMegaMarketId,
            "supplier.6@gmail.com",
            "Ngô Thị Mega - Nhà cung cấp",
            "0915000000",
            "Quản lý kho MM Mega Market"),
    ];

    internal static readonly ProductEntry[] Products =
    [
        new(
            Guid.Parse("bbbb00d1-0001-0001-0001-000000000001"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Nước ngọt Coca-Cola lon 320ml",
            "8934806010010",
            "Coca-Cola",
            "Nước giải khát có ga lon 320ml",
            12000m,
            180,
            120),
        new(
            Guid.Parse("bbbb00d1-0002-0002-0002-000000000002"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Trà Ô Long Tea+ không độ 455ml",
            "8934567012345",
            "Tea+",
            "Trà Ô long đóng chai 455ml",
            10000m,
            90,
            96),
        new(
            Guid.Parse("bbbb00d1-0003-0003-0003-000000000003"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("ccca0006-0006-0006-0006-000000000006"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Dầu ăn Neptune Light 1L",
            "8934701234567",
            "Neptune",
            "Dầu đậu nành tinh luyện 1 lít",
            68000m,
            120,
            48),
        new(
            Guid.Parse("bbbb00d1-0004-0004-0004-000000000004"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("ccca0004-0004-0004-0004-000000000004"),
            Guid.Parse("aaaa000a-000a-000a-000a-00000000000a"),
            "Gạo ST25 Co.op hạt ngọc trai 5kg",
            "8934712345678",
            "Co.op",
            "Gạo thơm ST25 túi 5kg",
            185000m,
            365,
            35),

        new(
            Guid.Parse("bbbb00d2-0001-0001-0001-000000000001"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Sữa tươi TH true MILK không đường 1L",
            "8934601234567",
            "TH true MILK",
            "Sữa tươi tiệt trùng không đường 1 lít",
            34000m,
            14,
            80),
        new(
            Guid.Parse("bbbb00d2-0002-0002-0002-000000000002"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("ccca000f-000f-000f-000f-00000000000f"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Mì Omachi sườn hầm gói lớn 80g",
            "8934723456789",
            "Omachi",
            "Mì ăn liền vị sườn hầm",
            8500m,
            240,
            200),
        new(
            Guid.Parse("bbbb00d2-0003-0003-0003-000000000003"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("ccca0006-0006-0006-0006-000000000006"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Nước mắm Nam Ngư 500ml",
            "8934734567890",
            "Nam Ngư",
            "Nước mắm nhĩ 500ml",
            42000m,
            730,
            60),
        new(
            Guid.Parse("bbbb00d2-0004-0004-0004-000000000004"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("ccca0004-0004-0004-0004-000000000004"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Kem đánh răng P/S bạc hà 180g",
            "8934745678901",
            "P/S",
            "Kem đánh răng bảo vệ nướu",
            45000m,
            730,
            72),

        new(
            Guid.Parse("bbbb00d3-0001-0001-0001-000000000001"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Bia Saigon Special 330ml",
            "8934756789012",
            "Sabeco",
            "Bia lon 330ml",
            15000m,
            180,
            144),
        new(
            Guid.Parse("bbbb00d3-0002-0002-0002-000000000002"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("ccca0007-0007-0007-0007-000000000007"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Snack Lay's vị tảo biển 95g",
            "8934767890123",
            "Lay's",
            "Khoai tây chiên 95g",
            32000m,
            120,
            64),
        new(
            Guid.Parse("bbbb00d3-0003-0003-0003-000000000003"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Nước tinh khiết Aquafina 1.5L",
            "8934778901234",
            "Aquafina",
            "Nước uống tinh khiết 1.5 lít",
            14000m,
            365,
            100),
        new(
            Guid.Parse("bbbb00d3-0004-0004-0004-000000000004"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0005-0005-0005-0005-000000000005"),
            "Phô mai Laughing Cow 8 miếng",
            "8934789012345",
            "Bel",
            "Phô mai tươi 8v",
            52000m,
            45,
            40),

        new(
            Guid.Parse("bbbb00e1-0001-0001-0001-000000000001"),
            SupermarketLotteId,
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Sữa dinh dưỡng Ensure Gold 237ml",
            "8934790123456",
            "Abbott",
            "Sữa dinh dưỡng dạng lỏng 237ml",
            48000m,
            180,
            36),
        new(
            Guid.Parse("bbbb00e1-0002-0002-0002-000000000002"),
            SupermarketLotteId,
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Sữa chua uống Yakult 5 chai",
            "8934801234567",
            "Yakult",
            "Sữa chua men sống 5x65ml",
            38000m,
            21,
            48),
        new(
            Guid.Parse("bbbb00e1-0003-0003-0003-000000000003"),
            SupermarketLotteId,
            Guid.Parse("ccca000e-000e-000e-000e-00000000000e"),
            Guid.Parse("aaaa0005-0005-0005-0005-000000000005"),
            "Bánh quy Oreo Original 133g",
            "8934812345678",
            "Oreo",
            "Bánh quy socola nhân kem 133g",
            28000m,
            200,
            56),
        new(
            Guid.Parse("bbbb00e1-0004-0004-0004-000000000004"),
            SupermarketLotteId,
            Guid.Parse("ccca0006-0006-0006-0006-000000000006"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Xốt mayonnaise Kewpie 450g",
            "8934823456789",
            "Kewpie",
            "Xốt mayonnaise Nhật Bản 450g",
            72000m,
            150,
            30),

        new(
            Guid.Parse("bbbb00e2-0001-0001-0001-000000000001"),
            SupermarketBachHoaXanhId,
            CategoryTofuEggId,
            UnitPieceId,
            "Trứng gà CP vỉ 10 trứng",
            "8934834567890",
            "CP",
            "Trứng gà công nghiệp vỉ 10 quả",
            3500m,
            14,
            80),
        new(
            Guid.Parse("bbbb00e2-0002-0002-0002-000000000002"),
            SupermarketBachHoaXanhId,
            Guid.Parse("ccca0002-0002-0002-0002-000000000002"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Thịt heo nạc vai 1kg",
            "8934845678901",
            "CP",
            "Thịt heo tươi nạc vai",
            125000m,
            3,
            25),
        new(
            Guid.Parse("bbbb00e2-0003-0003-0003-000000000003"),
            SupermarketBachHoaXanhId,
            Guid.Parse("ccca000d-000d-000d-000d-00000000000d"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Rau muống bó 500g",
            "8934856789012",
            "Dalat Garden",
            "Rau muống tươi bó 500g",
            12000m,
            2,
            60),
        new(
            Guid.Parse("bbbb00e2-0004-0004-0004-000000000004"),
            SupermarketBachHoaXanhId,
            Guid.Parse("ccca0008-0008-0008-0008-000000000008"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Chuối tiêu (kg)",
            "8934867890123",
            "VietGAP",
            "Chuối tiêu chín vàng",
            28000m,
            4,
            40),

        new(
            Guid.Parse("bbbb00e3-0001-0001-0001-000000000001"),
            SupermarketWinMartId,
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Pepsi lon 320ml",
            "8934878901234",
            "Pepsi",
            "Nước giải khát có ga lon 320ml",
            11000m,
            180,
            120),
        new(
            Guid.Parse("bbbb00e3-0002-0002-0002-000000000002"),
            SupermarketWinMartId,
            Guid.Parse("ccca0001-0001-0001-0001-000000000001"),
            Guid.Parse("aaaa0006-0006-0006-0006-000000000006"),
            "Sting dâu 330ml",
            "8934889012345",
            "Sting",
            "Nước tăng lực vị dâu 330ml",
            10000m,
            180,
            96),
        new(
            Guid.Parse("bbbb00e3-0003-0003-0003-000000000003"),
            SupermarketWinMartId,
            Guid.Parse("ccca0006-0006-0006-0006-000000000006"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Bột ngọt Ajinomoto 454g",
            "8934890123456",
            "Ajinomoto",
            "Bột ngọt umami 454g",
            38000m,
            730,
            45),
        new(
            Guid.Parse("bbbb00e3-0004-0004-0004-000000000004"),
            SupermarketWinMartId,
            Guid.Parse("ccca0002-0002-0002-0002-000000000002"),
            Guid.Parse("aaaa0007-0007-0007-0007-000000000007"),
            "Chả lụa Vissan gói 200g",
            "8934901234567",
            "Vissan",
            "Chả lụa tiệt trùng 200g",
            42000m,
            21,
            50),

        new(
            Guid.Parse("bbbb00e4-0001-0001-0001-000000000001"),
            SupermarketMegaMarketId,
            Guid.Parse("ccca0002-0002-0002-0002-000000000002"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Cá hồi Na Uy fillet 300g",
            "8934912345678",
            "Norwegian",
            "Phi lê cá hồi đông lạnh 300g",
            189000m,
            5,
            20),
        new(
            Guid.Parse("bbbb00e4-0002-0002-0002-000000000002"),
            SupermarketMegaMarketId,
            Guid.Parse("ccca0002-0002-0002-0002-000000000002"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Tôm sú đông lạnh 500g",
            "8934923456789",
            "Seafood King",
            "Tôm sú size 31/40 đông lạnh",
            145000m,
            7,
            30),
        new(
            Guid.Parse("bbbb00e4-0003-0003-0003-000000000003"),
            SupermarketMegaMarketId,
            Guid.Parse("ccca0008-0008-0008-0008-000000000008"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Xoài cát Hòa Lộc (kg)",
            "8934934567890",
            "Hòa Lộc",
            "Xoài cát chín cây",
            45000m,
            5,
            35),
        new(
            Guid.Parse("bbbb00e4-0004-0004-0004-000000000004"),
            SupermarketMegaMarketId,
            Guid.Parse("ccca0008-0008-0008-0008-000000000008"),
            Guid.Parse("aaaa0001-0001-0001-0001-000000000001"),
            "Dưa hấu ruột đỏ (kg)",
            "8934945678901",
            "Long An",
            "Dưa hấu ruột đỏ ngọt",
            22000m,
            4,
            50),
    ];

    internal static readonly StockLotEntry[] StockLots = BuildStockLots();

    private static readonly Dictionary<Guid, decimal> PriceByProductId =
        Products.ToDictionary(p => p.ProductId, p => p.OriginalPriceVnd);

    private static readonly Dictionary<Guid, Guid> UnitByProductId =
        Products.ToDictionary(p => p.ProductId, p => p.UnitId);

    internal static bool TryGetOriginalPrice(Guid productId, out decimal price) =>
        PriceByProductId.TryGetValue(productId, out price);

    internal static Guid ResolveUnitId(Guid productId) =>
        UnitByProductId.TryGetValue(productId, out var unitId) ? unitId : UnitPieceId;

    internal static IEnumerable<StockLotEntry> GetStockLotsForProduct(Guid productId) =>
        StockLots.Where(l => l.ProductId == productId);

    internal static decimal ResolveLotUnitPrice(ProductEntry product, Guid lotUnitId) =>
        ResolveLotUnitPrice(product.OriginalPriceVnd, product.UnitId, lotUnitId);

    internal static decimal ResolveLotUnitPrice(decimal productUnitPrice, Guid productUnitId, Guid lotUnitId)
    {
        if (lotUnitId == productUnitId)
            return productUnitPrice;

        if (productUnitId == UnitKgId && lotUnitId == UnitGramId)
            return Math.Max(1m, Math.Round(productUnitPrice / 1000m, 0, MidpointRounding.AwayFromZero));

        if (productUnitId == UnitBottleId && lotUnitId == UnitThungId)
            return Math.Round(productUnitPrice * 24m, 0, MidpointRounding.AwayFromZero);

        if (productUnitId == UnitCanId && lotUnitId == UnitThungId)
            return Math.Round(productUnitPrice * 24m, 0, MidpointRounding.AwayFromZero);

        if (productUnitId == UnitBoxId && lotUnitId == UnitPackId)
            return Math.Max(1m, Math.Round(productUnitPrice / 4m, 0, MidpointRounding.AwayFromZero));

        if (productUnitId == UnitPackId && lotUnitId == UnitBoxId)
            return Math.Round(productUnitPrice * 4m, 0, MidpointRounding.AwayFromZero);

        if (productUnitId == UnitPieceId && lotUnitId == UnitBoxId)
            return Math.Round(productUnitPrice * 12m, 0, MidpointRounding.AwayFromZero);

        if (productUnitId == UnitPieceId && lotUnitId == UnitPackId)
            return Math.Round(productUnitPrice * 10m, 0, MidpointRounding.AwayFromZero);

        if (lotUnitId == UnitPieceId && productUnitId is var pu && (pu == UnitKgId || pu == UnitBoxId || pu == UnitPackId))
            return Math.Max(1m, Math.Round(productUnitPrice / 2m, 0, MidpointRounding.AwayFromZero));

        return productUnitPrice;
    }

    private static int GetLotCountForProduct(ProductEntry product) =>
        product.ExpiryDays switch
        {
            <= 7 => 3,
            <= 20 => 4,
            _ => 5
        };

    private static Guid[] GetLotUnits(ProductEntry product) =>
        product.CategoryId == CategoryTofuEggId
            ? [UnitPieceId, UnitBoxId, UnitPieceId, UnitBoxId, UnitPieceId]
            : GetLotUnitsByPrimaryUnit(product.UnitId);

    private static Guid[] GetLotUnitsByPrimaryUnit(Guid primaryUnitId) =>
        primaryUnitId switch
        {
            var id when id == UnitKgId => [UnitKgId, UnitGramId, UnitKgId, UnitGramId, UnitKgId],
            var id when id == UnitBottleId => [UnitBottleId, UnitThungId, UnitBottleId, UnitThungId, UnitBottleId],
            var id when id == UnitCanId => [UnitCanId, UnitThungId, UnitCanId, UnitThungId, UnitCanId],
            var id when id == UnitBoxId => [UnitBoxId, UnitPieceId, UnitBoxId, UnitPieceId, UnitBoxId],
            var id when id == UnitPackId => [UnitPackId, UnitPieceId, UnitPackId, UnitPieceId, UnitPackId],
            var id when id == UnitPieceId => [UnitPieceId, UnitBoxId, UnitPieceId, UnitBoxId, UnitPieceId],
            _ => [primaryUnitId, UnitPieceId, UnitBoxId]
        };

    private static (int ExpiryDays, decimal Quantity) GetLotSchedule(ProductEntry product, int variant, int lotCount)
    {
        var nearExpiryDays = product.ExpiryDays <= 7
            ? Math.Max(1, product.ExpiryDays / 2)
            : Math.Max(1, (int)Math.Round(product.ExpiryDays * 0.12, MidpointRounding.AwayFromZero));

        var nearQty = Math.Max(1m, Math.Round(product.Quantity * 0.35m, MidpointRounding.AwayFromZero));

        return variant switch
        {
            1 => (product.ExpiryDays, product.Quantity),
            2 => (nearExpiryDays, nearQty),
            3 when product.ExpiryDays >= 21 => (
                Math.Min(365, (int)Math.Round(product.ExpiryDays * 1.6, MidpointRounding.AwayFromZero)),
                Math.Max(1m, Math.Round(product.Quantity * 0.55m, MidpointRounding.AwayFromZero))),
            3 => (
                Math.Max(1, product.ExpiryDays - 3),
                Math.Max(1m, Math.Round(product.Quantity * 0.48m, MidpointRounding.AwayFromZero))),
            4 => (
                Math.Max(1, (int)Math.Round(product.ExpiryDays * 0.45, MidpointRounding.AwayFromZero)),
                Math.Max(1m, Math.Round(product.Quantity * 0.42m, MidpointRounding.AwayFromZero))),
            _ => (
                Math.Max(1, product.ExpiryDays <= 7 ? 1 : (int)Math.Round(product.ExpiryDays * 0.08, MidpointRounding.AwayFromZero)),
                Math.Max(1m, Math.Round(product.Quantity * 0.22m, MidpointRounding.AwayFromZero)))
        };
    }

    private static decimal ScaleQuantityForUnit(ProductEntry product, Guid lotUnitId, decimal quantity)
    {
        if (product.UnitId == UnitKgId && lotUnitId == UnitGramId)
            return Math.Max(250m, Math.Round(quantity * 1000m, MidpointRounding.AwayFromZero));

        if (lotUnitId == UnitPieceId && product.UnitId is var u && (u == UnitKgId || u == UnitBoxId || u == UnitPackId))
            return Math.Max(1m, Math.Round(quantity / 2m, MidpointRounding.AwayFromZero));

        if (lotUnitId == UnitBagId)
            return Math.Max(1m, Math.Round(quantity / 3m, MidpointRounding.AwayFromZero));

        if (lotUnitId == UnitThungId && (product.UnitId == UnitBottleId || product.UnitId == UnitCanId))
            return Math.Max(1m, Math.Round(quantity / 24m, MidpointRounding.AwayFromZero));

        return quantity;
    }

    private static StockLotEntry[] BuildStockLots()
    {
        var lots = new List<StockLotEntry>();

        foreach (var product in Products)
        {
            var lotCount = GetLotCountForProduct(product);
            var units = GetLotUnits(product);

            for (var variant = 1; variant <= lotCount; variant++)
            {
                var unitId = units[(variant - 1) % units.Length];
                var schedule = GetLotSchedule(product, variant, lotCount);
                var quantity = ScaleQuantityForUnit(product, unitId, schedule.Quantity);
                var unitPrice = ResolveLotUnitPrice(product, unitId);

                lots.Add(new StockLotEntry(
                    ToLotId(product.ProductId, variant),
                    product.ProductId,
                    unitId,
                    schedule.ExpiryDays,
                    quantity,
                    ProductState.Published,
                    unitPrice));
            }
        }

        return lots.ToArray();
    }

    private static Guid ToLotId(Guid productId, int variant)
    {
        var text = productId.ToString();
        var lastDash = text.LastIndexOf('-');
        var prefix = "cccc" + text.Substring(4, lastDash - 4);
        return Guid.Parse($"{prefix}-{variant:D12}");
    }
}
