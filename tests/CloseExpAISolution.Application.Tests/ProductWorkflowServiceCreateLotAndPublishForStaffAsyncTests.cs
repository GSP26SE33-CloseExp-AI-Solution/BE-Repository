using System.Linq.Expressions;
using AiPricingRequest = CloseExpAISolution.Application.AIService.Models.PricingRequest;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.DTOs.Request;
using CloseExpAISolution.Application.DTOs.Response;
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
/// FN008 — UTCID01–UTCID11 per <c>.github/instructions/create-lot-and-publish-for-staff-async-test-sheet.md</c>
/// (<see cref="ProductWorkflowService.CreateLotAndPublishForStaffAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductWorkflowServiceCreateLotAndPublishForStaffAsyncTests"</c>
/// </summary>
public sealed class ProductWorkflowServiceCreateLotAndPublishForStaffAsyncTests
{
    private const string StaffName = "staff-test";

    private static readonly Guid SupermarketId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private sealed class WorkflowReposState
    {
        public StockLot? SavedLot { get; set; }
        public PricingHistory? SavedPricingHistory { get; set; }
    }

    private static Product VerifiedProduct(
        Guid productId,
        Guid supermarketId,
        Action<Product>? tweak = null)
    {
        var unitId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var p = new Product
        {
            ProductId = productId,
            SupermarketId = supermarketId,
            Status = ProductState.Verified,
            Name = "",
            Barcode = "",
            UnitId = unitId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CategoryRef = new Category { Name = "Snacks", CategoryId = Guid.NewGuid() },
            ProductDetail = new ProductDetail
            {
                ProductDetailId = Guid.NewGuid(),
                ProductId = productId,
                NutritionFacts = """{"Energy":"120"}"""
            }
        };
        tweak?.Invoke(p);
        return p;
    }

    private static UnitOfMeasure DefaultUnit(Guid unitId) =>
        new()
        {
            UnitId = unitId,
            Name = "Piece",
            Type = "count",
            Symbol = "pc",
            ConversionRate = 1
        };

    private static StaffCreateLotAndPublishRequestDto BaseRequest(Guid productId, Guid? unitId = null) =>
        new()
        {
            ProductId = productId,
            ExpiryDate = DateTime.UtcNow.AddDays(120),
            Quantity = 10,
            Weight = 1,
            OriginalUnitPrice = 100m,
            UnitId = unitId,
            IsManualFallback = false,
        };

    private static Mock<IUnitOfWork> BuildUow(
        Product? productForWorkflow,
        WorkflowReposState state,
        UnitOfMeasure defaultUnit,
        Guid? unknownUnitIdForResolutionFail = null)
    {
        var uow = new Mock<IUnitOfWork>();

        var productRepo = new Mock<IProductRepository>();
        productRepo
            .Setup(p => p.GetByIdWithWorkflowDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(productForWorkflow);
        uow.SetupGet(x => x.ProductRepository).Returns(productRepo.Object);

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.AddAsync(It.IsAny<StockLot>()))
            .Callback<StockLot>(l => state.SavedLot = l)
            .ReturnsAsync((StockLot l) => l);
        lotRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(() => state.SavedLot);
        lotRepo.Setup(r => r.Update(It.IsAny<StockLot>()))
            .Callback<StockLot>(l => state.SavedLot = l);
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var priceRepo = new Mock<IGenericRepository<PricingHistory>>();
        priceRepo.Setup(r => r.AddAsync(It.IsAny<PricingHistory>()))
            .Callback<PricingHistory>(h => state.SavedPricingHistory = h)
            .ReturnsAsync((PricingHistory h) => h);
        priceRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PricingHistory, bool>>>()))
            .ReturnsAsync(() => state.SavedPricingHistory);
        priceRepo.Setup(r => r.Update(It.IsAny<PricingHistory>()))
            .Callback<PricingHistory>(h => state.SavedPricingHistory = h);
        priceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<PricingHistory, bool>>>()))
            .ReturnsAsync(Array.Empty<PricingHistory>());
        uow.Setup(x => x.Repository<PricingHistory>()).Returns(priceRepo.Object);

        var unitRepo = new Mock<IGenericRepository<UnitOfMeasure>>();
        if (unknownUnitIdForResolutionFail.HasValue)
        {
            unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
                .ReturnsAsync(() => null);
        }
        else
        {
            unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
                .ReturnsAsync(() => defaultUnit);
        }

        unitRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<UnitOfMeasure> { defaultUnit });
        uow.Setup(x => x.Repository<UnitOfMeasure>()).Returns(unitRepo.Object);

        var cfgRepo = new Mock<IGenericRepository<SystemConfig>>();
        cfgRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<SystemConfig, bool>>>()))
            .ReturnsAsync(() => null);
        uow.Setup(x => x.Repository<SystemConfig>()).Returns(cfgRepo.Object);

        var aiLogRepo = new Mock<IGenericRepository<AIVerificationLog>>();
        aiLogRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AIVerificationLog, bool>>>()))
            .ReturnsAsync(Array.Empty<AIVerificationLog>());
        uow.Setup(x => x.Repository<AIVerificationLog>()).Returns(aiLogRepo.Object);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return uow;
    }

    private static ProductWorkflowService MakeSut(
        IUnitOfWork uow,
        IAIServiceClient ai,
        IMarketPriceService market,
        ILogger<ProductWorkflowService>? logger = null)
    {
        return new ProductWorkflowService(
            uow,
            ai,
            Mock.Of<IServiceProvider>(),
            market,
            Mock.Of<IBarcodeLookupService>(l =>
                l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) ==
                Task.FromResult<BarcodeProductInfo?>(null)),
            logger ?? Mock.Of<ILogger<ProductWorkflowService>>());
    }

    private static Mock<IMarketPriceService> PassiveMarketMock()
    {
        var m = new Mock<IMarketPriceService>();
        m.Setup(x => x.GetMarketPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
        return m;
    }

    private static Mock<IAIServiceClient> AiPricingOk(decimal suggestedPrice = 88m, float confidence = 0.85f)
    {
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingResponse
            {
                SuggestedPrice = suggestedPrice,
                Confidence = confidence,
                Reasons = new List<string> { "ai ok" },
            });
        return ai;
    }

    [Fact]
    public async Task UTCID01_ProductNull_KeyNotFound()
    {
        var pid = Guid.NewGuid();
        var state = new WorkflowReposState();
        var unit = DefaultUnit(Guid.NewGuid());
        var uow = BuildUow(null, state, unit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.CreateLotAndPublishForStaffAsync(BaseRequest(pid), SupermarketId, StaffName));

        Assert.Contains(pid.ToString(), ex.Message, StringComparison.Ordinal);
        Assert.Null(state.SavedLot);
    }

    [Fact]
    public async Task UTCID02_WrongSupermarket_UnauthorizedAccess()
    {
        var pid = Guid.NewGuid();
        var product = VerifiedProduct(pid, SupermarketId);
        var state = new WorkflowReposState();
        var unit = DefaultUnit(Guid.NewGuid());
        var uow = BuildUow(product, state, unit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var wrongSm = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.CreateLotAndPublishForStaffAsync(BaseRequest(pid), wrongSm, StaffName));

        Assert.Contains("siêu thị", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(state.SavedLot);
    }

    [Fact]
    public async Task UTCID03_ProductNotVerified_InvalidOperationFromCreateLot()
    {
        var pid = Guid.NewGuid();
        var product = VerifiedProduct(pid, SupermarketId, p => p.Status = ProductState.Draft);
        var state = new WorkflowReposState();
        var unit = DefaultUnit(Guid.NewGuid());
        var uow = BuildUow(product, state, unit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateLotAndPublishForStaffAsync(BaseRequest(pid), SupermarketId, StaffName));

        Assert.Contains("Verified", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(state.SavedLot);
    }

    [Fact]
    public async Task UTCID04_ShelfLifeTooShort_InvalidOperationAtCreate()
    {
        var pid = Guid.NewGuid();
        var product = VerifiedProduct(pid, SupermarketId);
        var state = new WorkflowReposState();
        var unit = DefaultUnit(Guid.NewGuid());
        var uow = BuildUow(product, state, unit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid);
        req.ExpiryDate = DateTime.UtcNow.AddHours(12);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName));

        Assert.Contains("shelf life", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(state.SavedLot);
    }

    [Fact]
    public async Task UTCID05_UnknownUnitId_InvalidOperation()
    {
        var pid = Guid.NewGuid();
        var product = VerifiedProduct(pid, SupermarketId);
        var badUnit = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var defaultUnit = DefaultUnit(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit, unknownUnitIdForResolutionFail: badUnit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid, badUnit);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName));

        Assert.Contains("UnitOfMeasure", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(state.SavedLot);
    }

    [Fact]
    public async Task UTCID06_ManualFallback_AppliesManualPricing_LogsInformation_NoAiPricingCall()
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);
        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit);

        var ai = new Mock<IAIServiceClient>();
        var market = PassiveMarketMock();
        var logger = new Mock<ILogger<ProductWorkflowService>>();
        var sut = MakeSut(uow.Object, ai.Object, market.Object, logger.Object);

        var req = BaseRequest(pid);
        req.IsManualFallback = true;

        var result = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName);

        Assert.True(result.IsManualFallback);
        Assert.False(result.TimeoutInfo.IsAiStep);
        Assert.Equal(ProductState.Published, result.StockLot.Status);
        Assert.Contains("Manual fallback", result.PricingSuggestion.Reasons.FirstOrDefault() ?? "", StringComparison.OrdinalIgnoreCase);

        ai.Verify(
            a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((s, _) =>
                    s.ToString()!.Contains("Manual pricing fallback", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID07_AiPricingBranch_GetLotPricingSuggestionInvokesAiClient()
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);
        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit);

        var ai = AiPricingOk(82m);
        var sut = MakeSut(uow.Object, ai.Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid);
        req.IsManualFallback = false;

        var result = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName);

        Assert.False(result.IsManualFallback);
        Assert.True(result.TimeoutInfo.IsAiStep);
        Assert.Equal(ProductState.Published, result.StockLot.Status);

        ai.Verify(
            a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        Assert.True(result.PricingSuggestion.SuggestedPrice > 0);
    }

    [Fact]
    public async Task UTCID08_AcceptedSuggestion_DefaultsFromFinalPricePresence()
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);

        async Task RunOnce(decimal? finalPrice, bool expectAccepted)
        {
            var state = new WorkflowReposState();
            var uow = BuildUow(product, state, defaultUnit);
            var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

            var req = BaseRequest(pid);
            req.IsManualFallback = true;
            req.FinalUnitPrice = finalPrice;
            req.AcceptedSuggestion = null;

            _ = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName);

            Assert.NotNull(state.SavedPricingHistory);
            Assert.Equal(expectAccepted, state.SavedPricingHistory!.AcceptedSuggestion);
        }

        await RunOnce(finalPrice: 55m, expectAccepted: false);
        await RunOnce(finalPrice: null, expectAccepted: true);
    }

    [Fact]
    public async Task UTCID09_EndToEnd_PublishedCategoryNutritionPricingSuggestion()
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("12121212-1212-1212-1212-121212121212");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);
        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit);

        var sut = MakeSut(uow.Object, AiPricingOk(90m).Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid);

        var result = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName);

        Assert.Equal(ProductState.Published, result.StockLot.Status);
        Assert.Equal("Snacks", result.ProductCategory);
        Assert.NotNull(result.ProductNutritionFacts);
        Assert.True(result.ProductNutritionFacts!.TryGetValue("Energy", out var ev) && ev == "120");
        Assert.True(result.PricingSuggestionResolvedBeforePublish);
        Assert.True(result.PricingSuggestion.SuggestedPrice > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UTCID10_TimeoutInfo_IsAiStep_NotManual(bool isManualFallback)
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("abababab-abab-abab-abab-abababababab");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);
        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit);

        var sut = MakeSut(uow.Object, AiPricingOk().Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid);
        req.IsManualFallback = isManualFallback;

        var result = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName);

        Assert.Equal(30, result.TimeoutInfo.TimeoutSeconds);
        Assert.True(result.TimeoutInfo.SupportsManualFallback);
        Assert.Equal(!isManualFallback, result.TimeoutInfo.IsAiStep);
    }

    [Fact]
    public async Task UTCID11_SameCancellationToken_OnSaveChangesAndAiPricing()
    {
        var pid = Guid.NewGuid();
        var unitId = Guid.Parse("bcbcbcbc-bcbc-bcbc-bcbc-bcbcbcbcbcbc");
        var product = VerifiedProduct(pid, SupermarketId);
        var defaultUnit = DefaultUnit(unitId);
        var state = new WorkflowReposState();
        var uow = BuildUow(product, state, defaultUnit);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var ai = AiPricingOk();
        ai.Setup(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), ct))
            .ReturnsAsync(new PricingResponse
            {
                SuggestedPrice = 77m,
                Confidence = 0.8f,
                Reasons = new List<string> { "tok" },
            });

        var sut = MakeSut(uow.Object, ai.Object, PassiveMarketMock().Object);

        var req = BaseRequest(pid);

        _ = await sut.CreateLotAndPublishForStaffAsync(req, SupermarketId, StaffName, ct);

        uow.Verify(x => x.SaveChangesAsync(ct), Times.AtLeastOnce);
        ai.Verify(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), ct), Times.AtLeastOnce);
    }
}
