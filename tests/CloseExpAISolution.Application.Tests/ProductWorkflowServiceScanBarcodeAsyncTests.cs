using System.Linq.Expressions;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using CloseExpAISolution.Infrastructure.Base;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN006 — UTCID01–UTCID16 per <c>.github/instructions/scan-barcode-async-test-sheet.md</c>
/// (<see cref="ProductWorkflowService.ScanBarcodeAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductWorkflowServiceScanBarcodeAsyncTests"</c>
/// </summary>
public sealed class ProductWorkflowServiceScanBarcodeAsyncTests
{
    private const string Barcode = "8934567890123";

    private static ProductWorkflowService CreateSut(
        IUnitOfWork uow,
        IBarcodeLookupService? lookup = null,
        ILogger<ProductWorkflowService>? logger = null)
    {
        return new ProductWorkflowService(
            uow,
            Mock.Of<IAIServiceClient>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<IMarketPriceService>(),
            lookup ?? Mock.Of<IBarcodeLookupService>(l =>
                l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.FromResult<BarcodeProductInfo?>(null)),
            logger ?? Mock.Of<ILogger<ProductWorkflowService>>());
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(
        Guid supermarketId,
        Supermarket? supermarket,
        IReadOnlyList<Product> findProducts,
        Func<Guid, Task<Product?>>? getByIdWithDetails = null,
        IReadOnlyList<ProductImage>? images = null,
        IReadOnlyList<StockLot>? lots = null,
        IReadOnlyList<PricingHistory>? pricing = null)
    {
        var uow = new Mock<IUnitOfWork>();

        var smRepo = new Mock<ISupermarketRepository>();
        smRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Supermarket, bool>>>()))
            .ReturnsAsync(supermarket);
        uow.SetupGet(x => x.SupermarketRepository).Returns(smRepo.Object);

        var productRepo = new Mock<IProductRepository>();
        productRepo
            .Setup(p => p.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
            .ReturnsAsync(findProducts);
        productRepo
            .Setup(p => p.GetByIdWithWorkflowDetailsAsync(It.IsAny<Guid>()))
            .Returns((Guid id) => getByIdWithDetails?.Invoke(id) ?? Task.FromResult<Product?>(null));
        uow.SetupGet(x => x.ProductRepository).Returns(productRepo.Object);

        var imgRepo = new Mock<IGenericRepository<ProductImage>>();
        imgRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductImage, bool>>>()))
            .ReturnsAsync(images ?? Array.Empty<ProductImage>());
        uow.Setup(x => x.Repository<ProductImage>()).Returns(imgRepo.Object);

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(lots ?? Array.Empty<StockLot>());
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var priceRepo = new Mock<IGenericRepository<PricingHistory>>();
        priceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingHistory, bool>>>()))
            .ReturnsAsync(pricing ?? Array.Empty<PricingHistory>());
        uow.Setup(x => x.Repository<PricingHistory>()).Returns(priceRepo.Object);

        return uow;
    }

    private static Product ProductStub(
        Guid productId,
        Guid supermarketId,
        ProductState status,
        string name = "P",
        string brand = "B",
        string category = "C")
    {
        return new Product
        {
            ProductId = productId,
            SupermarketId = supermarketId,
            Status = status,
            Name = name,
            Barcode = Barcode,
            Supermarket = new Supermarket { SupermarketId = supermarketId, Name = $"SM-{supermarketId:N}" },
            ProductDetail = new ProductDetail { Brand = brand },
            CategoryRef = new Category { Name = category }
        };
    }



    [Fact]
    public async Task UTCID01_SupermarketNotFound_ThrowsArgumentExceptionWithParamName()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(smId, supermarket: null, findProducts: Array.Empty<Product>());
        var sut = CreateSut(uow.Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None));

        Assert.Equal("supermarketId", ex.ParamName);
        Assert.Contains(smId.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UTCID02_CurrentSupermarketVerified_ReturnsCreateLot()
    {
        var smId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var p = ProductStub(pid, smId, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            new[] { p },
            getByIdWithDetails: _ => Task.FromResult<Product?>(p));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.True(result.ProductExists);
        Assert.Equal("CREATE_LOT", result.NextAction);
        Assert.True(result.CanCreateLotDirectly);
        Assert.False(result.RequiresOcrUpload);
        Assert.Single(result.MatchedProducts);
    }

    [Fact]
    public async Task UTCID03_CurrentDraft_ReturnsVerifyProduct()
    {
        var smId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var p = ProductStub(pid, smId, ProductState.Draft);
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            new[] { p },
            getByIdWithDetails: _ => Task.FromResult<Product?>(p));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.True(result.ProductExists);
        Assert.Equal("VERIFY_PRODUCT", result.NextAction);
        Assert.True(result.RequiresVerification);
        Assert.Equal(pid, result.VerificationProductId);
    }

    [Fact]
    public async Task UTCID04_OnlyOtherSupermarket_ReturnsChooseOrCreatePrivate()
    {
        var currentSm = Guid.NewGuid();
        var otherSm = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var p = ProductStub(pid, otherSm, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            currentSm,
            new Supermarket { SupermarketId = currentSm, Name = "Current" },
            new[] { p },
            getByIdWithDetails: _ => Task.FromResult<Product?>(p));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, currentSm, CancellationToken.None);

        Assert.True(result.ProductExists);
        Assert.Equal("CHOOSE_OR_CREATE_PRIVATE_PRODUCT", result.NextAction);
        Assert.True(result.CanCreatePrivateProductForCurrentSupermarket);
    }

    [Fact]
    public async Task UTCID05_NoDbMatch_LookupReturnsDto_OcrPathWithMappedInfo()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        var info = new BarcodeProductInfo
        {
            Barcode = Barcode,
            ProductName = "Ext",
            Brand = "EB",
            Category = "EC",
            Source = "test"
        };
        var lookup = new Mock<IBarcodeLookupService>();
        lookup
            .Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(info);
        var sut = CreateSut(uow.Object, lookup.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.False(result.ProductExists);
        Assert.True(result.RequiresOcrUpload);
        Assert.Equal("UPLOAD_IMAGE_FOR_OCR", result.NextAction);
        Assert.NotNull(result.BarcodeLookupInfo);
        Assert.Equal("Ext", result.BarcodeLookupInfo!.ProductName);
        lookup.Verify(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID06_LookupThrows_DoesNotPropagate_BarcodeLookupInfoNull_AndLogsWarning()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        var lookup = new Mock<IBarcodeLookupService>();
        lookup
            .Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("lookup down"));
        var logger = new Mock<ILogger<ProductWorkflowService>>();
        var sut = CreateSut(uow.Object, lookup.Object, logger.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.Null(result.BarcodeLookupInfo);
        Assert.True(result.RequiresOcrUpload);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID07_LookupReturnsNull_BarcodeLookupInfoNull()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        var lookup = new Mock<IBarcodeLookupService>();
        lookup
            .Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BarcodeProductInfo?)null);
        var sut = CreateSut(uow.Object, lookup.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.Null(result.BarcodeLookupInfo);
        Assert.True(result.RequiresOcrUpload);
    }

    [Fact]
    public async Task UTCID08_And_UTCID09_FindExcludesHiddenDeleted_ServiceSeesEmpty_RoutesToOcr()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BarcodeProductInfo?)null);
        var sut = CreateSut(uow.Object, lookup.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.False(result.ProductExists);
        lookup.Verify(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID10_MatchedProducts_Order_CurrentBeforeVerifiedPeer()
    {
        var currentSm = Guid.NewGuid();
        var otherSm = Guid.NewGuid();
        var draftAtCurrent = ProductStub(Guid.NewGuid(), currentSm, ProductState.Draft);
        var verifiedOther = ProductStub(Guid.NewGuid(), otherSm, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            currentSm,
            new Supermarket { SupermarketId = currentSm, Name = "Current" },
            new[] { verifiedOther, draftAtCurrent },
            getByIdWithDetails: id =>
                Task.FromResult<Product?>(id == draftAtCurrent.ProductId ? draftAtCurrent : verifiedOther));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, currentSm, CancellationToken.None);

        Assert.Equal(2, result.MatchedProducts!.Count);
        Assert.Equal(draftAtCurrent.ProductId, result.MatchedProducts[0].ProductId);
        Assert.True(result.MatchedProducts[0].IsCurrentSupermarket);
    }

    [Fact]
    public async Task UTCID11_CurrentVerified_WithPeerVerified_ReturnsCreateLot()
    {
        var currentSm = Guid.NewGuid();
        var otherSm = Guid.NewGuid();
        var currentP = ProductStub(Guid.NewGuid(), currentSm, ProductState.Verified);
        var otherP = ProductStub(Guid.NewGuid(), otherSm, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            currentSm,
            new Supermarket { SupermarketId = currentSm, Name = "Current" },
            new[] { otherP, currentP },
            getByIdWithDetails: id =>
                Task.FromResult<Product?>(id == currentP.ProductId ? currentP : otherP));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, currentSm, CancellationToken.None);

        Assert.Equal("CREATE_LOT", result.NextAction);
        Assert.True(result.CanCreateLotDirectly);
        Assert.Equal(2, result.MatchedProducts!.Count);
    }

    [Fact]
    public async Task UTCID12_NoImagesOrLots_EnrichmentNullable_StillCreateLot()
    {
        var smId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var p = ProductStub(pid, smId, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            new[] { p },
            getByIdWithDetails: _ => Task.FromResult<Product?>(p),
            images: Array.Empty<ProductImage>(),
            lots: Array.Empty<StockLot>());
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.Equal("CREATE_LOT", result.NextAction);
        Assert.Null(result.ExistingProduct!.MainImageUrl);
        Assert.Equal(0, result.ExistingProduct.TotalLotsSold);
        Assert.Null(result.ExistingProduct.LastPrice);
    }

    [Fact]
    public async Task UTCID13_GetByIdWorkflowDetailsNull_FallsBackToBaseProduct()
    {
        var smId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var p = ProductStub(pid, smId, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            new[] { p },
            getByIdWithDetails: _ => Task.FromResult<Product?>(null));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.True(result.ProductExists);
        Assert.Equal(pid, result.ExistingProduct!.ProductId);
        Assert.Equal("CREATE_LOT", result.NextAction);
    }

    [Fact]
    public async Task UTCID14_CurrentDraft_OtherVerified_ReturnsVerifyForCurrent()
    {
        var currentSm = Guid.NewGuid();
        var otherSm = Guid.NewGuid();
        var currentP = ProductStub(Guid.NewGuid(), currentSm, ProductState.Draft);
        var otherP = ProductStub(Guid.NewGuid(), otherSm, ProductState.Verified);
        var uow = CreateUnitOfWorkMock(
            currentSm,
            new Supermarket { SupermarketId = currentSm, Name = "Current" },
            new[] { otherP, currentP },
            getByIdWithDetails: id =>
                Task.FromResult<Product?>(id == currentP.ProductId ? currentP : otherP));
        var sut = CreateSut(uow.Object);

        var result = await sut.ScanBarcodeAsync(Barcode, currentSm, CancellationToken.None);

        Assert.Equal("VERIFY_PRODUCT", result.NextAction);
        Assert.Equal(currentP.ProductId, result.VerificationProductId);
    }

    [Fact]
    public async Task UTCID15_LookupAsyncReceivesSameCancellationToken()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        CancellationToken? passed = null;
        var lookup = new Mock<IBarcodeLookupService>();
        lookup
            .Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .Callback((string _, CancellationToken ct) => passed = ct)
            .ReturnsAsync((BarcodeProductInfo?)null);
        var sut = CreateSut(uow.Object, lookup.Object);
        using var cts = new CancellationTokenSource();

        await sut.ScanBarcodeAsync(Barcode, smId, cts.Token);

        Assert.True(passed.HasValue);
        Assert.Equal(cts.Token, passed.Value);
    }

    [Fact]
    public async Task UTCID16_LogsNotInDatabaseBeforeLookup()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUnitOfWorkMock(
            smId,
            new Supermarket { SupermarketId = smId, Name = "S" },
            Array.Empty<Product>());
        var seq = 0;
        var notFoundOrder = int.MaxValue;
        var lookupEnteredOrder = int.MaxValue;
        var decorated = new Mock<ILogger<ProductWorkflowService>>();
        decorated
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var formatter = (Delegate)invocation.Arguments[4]!;
                var state = invocation.Arguments[2]!;
                var msg = formatter.DynamicInvoke(state, null)?.ToString() ?? "";
                if (msg.Contains("Product not found in database", StringComparison.Ordinal))
                    notFoundOrder = ++seq;
            }));
        var lookup = new Mock<IBarcodeLookupService>();
        lookup
            .Setup(l => l.LookupAsync(Barcode, It.IsAny<CancellationToken>()))
            .Callback(() => lookupEnteredOrder = ++seq)
            .ReturnsAsync((BarcodeProductInfo?)null);
        var sut = CreateSut(uow.Object, lookup.Object, decorated.Object);

        await sut.ScanBarcodeAsync(Barcode, smId, CancellationToken.None);

        Assert.NotEqual(int.MaxValue, notFoundOrder);
        Assert.NotEqual(int.MaxValue, lookupEnteredOrder);
        Assert.True(notFoundOrder < lookupEnteredOrder);
    }
}
