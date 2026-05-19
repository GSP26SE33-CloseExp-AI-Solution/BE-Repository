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
/// FN009 — UTCID01–UTCID12 per <c>.github/instructions/get-lot-pricing-suggestion-async-test-sheet.md</c>
/// (<see cref="ProductWorkflowService.GetLotPricingSuggestionAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductWorkflowServiceGetLotPricingSuggestionAsyncTests"</c>
/// </summary>
public sealed class ProductWorkflowServiceGetLotPricingSuggestionAsyncTests
{
    private sealed class PricingRowState
    {
        public PricingHistory? Row { get; set; }
        public int AddAsyncInvocations { get; set; }
    }

    private static GetPricingSuggestionRequestDto Req(decimal price = 100m) =>
        new() { OriginalPrice = price };

    private static ProductWorkflowService MakeSut(
        IUnitOfWork uow,
        IAIServiceClient ai,
        IMarketPriceService market,
        ILogger<ProductWorkflowService>? log = null)
    {
        return new ProductWorkflowService(
            uow,
            ai,
            Mock.Of<IServiceProvider>(),
            market,
            Mock.Of<IBarcodeLookupService>(b =>
                b.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) ==
                Task.FromResult<BarcodeProductInfo?>(null)),
            log ?? Mock.Of<ILogger<ProductWorkflowService>>());
    }

    private static Mock<IUnitOfWork> BuildUow(
        Guid lotId,
        StockLot? lot,
        Product? product,
        PricingRowState pricingState)
    {
        var uow = new Mock<IUnitOfWork>();

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(() => lot);
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var productRepo = new Mock<IProductRepository>();
        productRepo
            .Setup(p => p.GetByIdWithWorkflowDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(product);
        uow.SetupGet(x => x.ProductRepository).Returns(productRepo.Object);

        var priceRepo = new Mock<IGenericRepository<PricingHistory>>();
        priceRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PricingHistory, bool>>>()))
            .ReturnsAsync(() => pricingState.Row);
        priceRepo.Setup(r => r.AddAsync(It.IsAny<PricingHistory>()))
            .Callback<PricingHistory>(_ => pricingState.AddAsyncInvocations++)
            .ReturnsAsync((PricingHistory h) =>
            {
                pricingState.Row = h;
                return h;
            });
        priceRepo.Setup(r => r.Update(It.IsAny<PricingHistory>()))
            .Callback<PricingHistory>(h => pricingState.Row = h);
        uow.Setup(x => x.Repository<PricingHistory>()).Returns(priceRepo.Object);

        var aiVerRepo = new Mock<IGenericRepository<AIVerificationLog>>();
        aiVerRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<AIVerificationLog, bool>>>()))
            .ReturnsAsync(Array.Empty<AIVerificationLog>());
        uow.Setup(x => x.Repository<AIVerificationLog>()).Returns(aiVerRepo.Object);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return uow;
    }

    private static StockLot LotStub(Guid lotId, Guid productId, DateTime? expiryUtc = null) =>
        new()
        {
            LotId = lotId,
            ProductId = productId,
            ExpiryDate = expiryUtc ?? DateTime.UtcNow.AddDays(22),
            ManufactureDate = DateTime.UtcNow.AddDays(-5),
            Status = ProductState.Draft,
            Quantity = 6,
            Weight = 1,
            OriginalUnitPrice = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    private static Product ProductStub(
        Guid productId,
        string name,
        string barcode,
        Guid? supermarketId = null)
    {
        var sm = supermarketId ?? Guid.NewGuid();
        return new Product
        {
            ProductId = productId,
            SupermarketId = sm,
            Name = name,
            Barcode = barcode,
            Status = ProductState.Verified,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CategoryRef = new Category { CategoryId = Guid.NewGuid(), Name = "Produce" },
            ProductDetail = new ProductDetail { ProductDetailId = Guid.NewGuid(), ProductId = productId, Brand = "B" },
        };
    }

    private static Mock<IAIServiceClient> AiHappy(decimal suggested = 72m)
    {
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingResponse
            {
                SuggestedPrice = suggested,
                Confidence = 0.92f,
                Reasons = new List<string> { "ai path" },
            });
        return ai;
    }

    private static Mock<IMarketPriceService> PassiveMarket()
    {
        var m = new Mock<IMarketPriceService>();
        m.Setup(x => x.GetMarketPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null);
        return m;
    }

    private static MarketPriceResult MarketWithDetails(string barcodePrefix = "")
    {
        return new MarketPriceResult
        {
            MinPrice = 10,
            MaxPrice = 22,
            AvgPrice = 16,
            Details = new List<MarketPriceDetail>
            {
                new()
                {
                    Source = barcodePrefix + "SRC",
                    StoreName = "T",
                    Price = 16,
                    CollectedAt = DateTime.UtcNow,
                    IsInStock = true,
                },
            },
        };
    }

    [Fact]
    public async Task UTCID01_StockLotNull_KeyNotFound()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();

        var uow = BuildUow(lotId, null, ProductStub(pid, "x", ""), state);
        var sut = MakeSut(uow.Object, AiHappy().Object, PassiveMarket().Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetLotPricingSuggestionAsync(lotId, Req()));

        Assert.Contains(lotId.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, state.AddAsyncInvocations);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID02_ProductNull_KeyNotFound()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);

        var uow = BuildUow(lotId, lot, null, state);
        var sut = MakeSut(uow.Object, AiHappy().Object, PassiveMarket().Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetLotPricingSuggestionAsync(lotId, Req()));

        Assert.Contains(pid.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, state.AddAsyncInvocations);
    }

    [Fact]
    public async Task UTCID03_NoPricingHistoryRow_AddIssuedBeforeInternal_ThenSynced()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState { Row = null };
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Tea", "");

        var uow = BuildUow(lotId, lot, product, state);
        var sut = MakeSut(uow.Object, AiHappy(91m).Object, PassiveMarket().Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(100));

        Assert.Equal(1, state.AddAsyncInvocations);
        Assert.NotNull(state.Row);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(result.SuggestedPrice, state.Row!.SuggestedPrice);
        Assert.Equal(result.Confidence, (float)state.Row.AIConfidence);
    }

    [Fact]
    public async Task UTCID04_PreExistingPricingHistory_NoAdd_UpdateOnlyPersist()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();

        var existing = new PricingHistory
        {
            AIPriceId = Guid.NewGuid(),
            LotId = lotId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            SuggestedPrice = 1,
            Reason = "old",
        };

        var state = new PricingRowState { Row = existing };
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Juice", "893");

        var uow = BuildUow(lotId, lot, product, state);
        var sut = MakeSut(uow.Object, AiHappy(50m).Object, PassiveMarket().Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(120));

        Assert.Equal(0, state.AddAsyncInvocations);
        Assert.Equal(result.SuggestedPrice, state.Row!.SuggestedPrice);
    }

    [Fact]
    public async Task UTCID05_NoBarcodeNorName_NoMarketServiceCalls_NoSources()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "", "");

        var uow = BuildUow(lotId, lot, product, state);
        var market = PassiveMarket();

        var sut = MakeSut(uow.Object, AiHappy().Object, market.Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req());

        Assert.Empty(result.MarketPriceSources);
        market.Verify(
            m => m.GetMarketPriceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        market.Verify(
            m => m.TriggerCrawlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UTCID06_GetMarketPricesWithDetails_NoCrawl_AIUsesMarketBands()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Milk", "111");

        var uow = BuildUow(lotId, lot, product, state);
        var m = PassiveMarket();

        var detailResult = MarketWithDetails();
        m.Setup(x => x.GetMarketPriceAsync("111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResult);
        var sut = MakeSut(uow.Object, AiHappy(81m).Object, m.Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(100));

        Assert.True(result.MarketPriceSources.Count > 0);
        Assert.Equal(10m, result.MinMarketPrice);
        Assert.Equal(16m, result.AvgMarketPrice);
        m.Verify(
            x => x.TriggerCrawlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(16m, state.Row!.MarketAvgPrice);
    }

    [Fact]
    public async Task UTCID07_EmptyMarketThenSuccessfulCrawl_RefetchesDetails()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Yogurt", "222");

        var uow = BuildUow(lotId, lot, product, state);
        var m = PassiveMarket();

        var full = MarketWithDetails();

        var emptyDb = new MarketPriceResult { Details = new List<MarketPriceDetail>() };
        var afterCrawlReturns = MarketWithDetails();

        m.SetupSequence(x => x.GetMarketPriceAsync("222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyDb)
            .ReturnsAsync(full);

        m.Setup(x =>
                x.TriggerCrawlAsync("222", "Yogurt", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult { Success = true, PricesFound = 5, Error = null });

        var sut = MakeSut(uow.Object, AiHappy(71m).Object, m.Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(111));

        m.Verify(x => x.TriggerCrawlAsync("222", "Yogurt", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(result.MarketPriceSources);

        Mock.Get(uow.Object).Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Fact]
    public async Task UTCID08_CrawlFails_StillCompletesWithAiPricing_LogWarning()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Chips", "333");

        var uow = BuildUow(lotId, lot, product, state);
        var m = PassiveMarket();

        m.SetupSequence(x => x.GetMarketPriceAsync("333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MarketPriceResult { Details = new() });

        m.Setup(x =>
                x.TriggerCrawlAsync("333", "Chips", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrawlResult { Success = false, PricesFound = 0, Error = "net err" });

        var log = new Mock<ILogger<ProductWorkflowService>>();
        var sut = MakeSut(uow.Object, AiHappy(93m).Object, m.Object, log.Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(103));

        Assert.Contains("ai path", string.Join(",", result.Reasons), StringComparison.OrdinalIgnoreCase);
        log.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((s, _) => s.ToString()!.Contains("Crawl failed", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID09_GetMarketPriceThrows_AIStillRuns_LogWarning()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Jam", "444");

        var uow = BuildUow(lotId, lot, product, state);

        var m = PassiveMarket();
        m.Setup(x => x.GetMarketPriceAsync("444", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var log = new Mock<ILogger<ProductWorkflowService>>();
        var sut = MakeSut(uow.Object, AiHappy(60m).Object, m.Object, log.Object);

        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(140));

        Assert.Equal(60m, result.SuggestedPrice);
        log.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((s, _) => s.ToString()!.Contains("Error getting market prices", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID10_DiscountMappedFromSuggestedVsOriginalPrice()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid);
        var product = ProductStub(pid, "Bread", "");

        var uow = BuildUow(lotId, lot, product, state);
        var ai = AiHappy(77m);

        var sut = MakeSut(uow.Object, ai.Object, PassiveMarket().Object);

        var original = 100m;
        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(original));

        Assert.Equal(23.0m, result.DiscountPercent);
        ai.Verify(
            a => a.GetPriceSuggestionAsync(
                It.Is<AiPricingRequest>(r =>
                    Math.Abs(r.BasePrice - original) < 0.0001m
                    && r.ProductName == "Bread"),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task UTCID11_AIReturnsNull_FallbackDiscountAndConfidence()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();

        var state = new PricingRowState();
        var lot = LotStub(lotId, pid, expiryUtc: DateTime.UtcNow.AddDays(20));

        var product = ProductStub(pid, "Snack", "");

        var uow = BuildUow(lotId, lot, product, state);

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingResponse?)null);

        var sut = MakeSut(uow.Object, ai.Object, PassiveMarket().Object);

        var origin = 200m;
        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(origin));

        Assert.Equal(0.5f, result.Confidence);
        Assert.Single(result.Reasons);
        Assert.Contains("Fallback calculation", result.Reasons[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal(15m, result.DiscountPercent);
        Assert.Equal(origin * (1 - 15m / 100m), result.SuggestedPrice);

        Assert.Equal(result.SuggestedPrice, state.Row!.SuggestedPrice);
    }

    [Fact]
    public async Task UTCID12_AIThrows_LogErrorFallback_CancellationForwarded()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var state = new PricingRowState();
        var lot = LotStub(lotId, pid, expiryUtc: DateTime.UtcNow.AddMonths(10));
        var product = ProductStub(pid, "Oil", "");

        var uow = BuildUow(lotId, lot, product, state);

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), ct))
            .ThrowsAsync(new IOException("boom"));

        var log = new Mock<ILogger<ProductWorkflowService>>();
        var sut = MakeSut(uow.Object, ai.Object, PassiveMarket().Object, log.Object);

        var origin = 50m;
        var result = await sut.GetLotPricingSuggestionAsync(lotId, Req(origin), ct);

        Assert.Equal(10m, result.DiscountPercent);
        Assert.Equal(0.5f, result.Confidence);

        ai.Verify(a => a.GetPriceSuggestionAsync(It.IsAny<AiPricingRequest>(), ct), Times.AtLeastOnce());
        Mock.Get(uow.Object).Verify(x => x.SaveChangesAsync(ct), Times.AtLeastOnce);

        log.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((s, _) =>
                    s.ToString()!.Contains("Error calling AI pricing", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
