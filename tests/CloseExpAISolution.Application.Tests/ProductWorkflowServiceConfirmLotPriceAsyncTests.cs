using System.Linq.Expressions;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.DTOs.Request;
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
/// FN010 — UTCID01–UTCID10 per <c>.github/instructions/confirm-lot-price-async-test-sheet.md</c>
/// (<see cref="ProductWorkflowService.ConfirmLotPriceAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductWorkflowServiceConfirmLotPriceAsyncTests"</c>
/// </summary>
public sealed class ProductWorkflowServiceConfirmLotPriceAsyncTests
{
    private static ProductWorkflowService MakeSut(IUnitOfWork uow) =>
        new ProductWorkflowService(
            uow,
            Mock.Of<IAIServiceClient>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<IMarketPriceService>(),
            Mock.Of<IBarcodeLookupService>(b =>
                b.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) ==
                Task.FromResult<BarcodeProductInfo?>(null)),
            Mock.Of<ILogger<ProductWorkflowService>>());

    private static Mock<IUnitOfWork> BuildUow(
        Guid lotId,
        StockLot? lot,
        Product? product,
        PricingHistory? history,
        Mock<IGenericRepository<UnitOfMeasure>>? unitRepo = null)
    {
        var uow = new Mock<IUnitOfWork>();

        var lotRepo = new Mock<IGenericRepository<StockLot>>();
        lotRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<StockLot, bool>>>()))
            .ReturnsAsync(() => lot);
        lotRepo.Setup(r => r.Update(It.IsAny<StockLot>()))
            .Callback<StockLot>(l => lot = l);
        uow.Setup(x => x.Repository<StockLot>()).Returns(lotRepo.Object);

        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(p => p.GetByIdWithWorkflowDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => product);
        uow.SetupGet(x => x.ProductRepository).Returns(productRepo.Object);

        var pricingRepo = new Mock<IGenericRepository<PricingHistory>>();
        pricingRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<PricingHistory, bool>>>()))
            .ReturnsAsync(() => history);
        pricingRepo.Setup(r => r.Update(It.IsAny<PricingHistory>()))
            .Callback<PricingHistory>(h => history = h);
        uow.Setup(x => x.Repository<PricingHistory>()).Returns(pricingRepo.Object);

        if (unitRepo == null)
        {
            unitRepo = new Mock<IGenericRepository<UnitOfMeasure>>();
            unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
                .ReturnsAsync(() => null);
        }

        uow.Setup(x => x.Repository<UnitOfMeasure>()).Returns(unitRepo.Object);

        uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return uow;
    }

    private static StockLot Lot(Guid lotId, Guid productId, Guid unitId, UnitOfMeasure? navUnit = null) =>
        new()
        {
            LotId = lotId,
            ProductId = productId,
            UnitId = unitId,
            ExpiryDate = DateTime.UtcNow.AddDays(10),
            ManufactureDate = DateTime.UtcNow.AddDays(-1),
            Quantity = 1,
            Weight = 1,
            OriginalUnitPrice = 100,
            Status = ProductState.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Unit = navUnit,
        };

    private static UnitOfMeasure UnitNav(Guid unitId) =>
        new()
        {
            UnitId = unitId,
            Name = "Kg",
            Type = "weight",
            Symbol = "kg",
            ConversionRate = 1,
        };

    private static Product ProductNav(Guid pid, Guid? unitId = null)
    {
        var u = unitId ?? Guid.NewGuid();
        return new Product
        {
            ProductId = pid,
            SupermarketId = Guid.NewGuid(),
            UnitId = u,
            Name = "P1",
            Barcode = "x",
            Status = ProductState.Verified,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private static PricingHistory HistoryRow(Guid lotId, decimal suggested = 100m, decimal aiConf = 0.75m) =>
        new()
        {
            AIPriceId = Guid.NewGuid(),
            LotId = lotId,
            SuggestedPrice = suggested,
            AIConfidence = aiConf,
            Reason = "test",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
        };

    [Fact]
    public async Task UTCID01_StockLotMissing_KeyNotFound()
    {
        var lotId = Guid.NewGuid();

        var uow = BuildUow(lotId, null, ProductNav(Guid.NewGuid()), HistoryRow(lotId));
        var sut = MakeSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ConfirmLotPriceAsync(lotId, new ConfirmPriceRequestDto { ConfirmedBy = "s" }));

        Assert.Contains(lotId.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID02_ProductMissing_KeyNotFound()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();

        var u = Guid.NewGuid();
        var lot = Lot(lotId, pid, u, UnitNav(u));

        var uow = BuildUow(lotId, lot, null, HistoryRow(lotId));
        var sut = MakeSut(uow.Object);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.ConfirmLotPriceAsync(lotId, new ConfirmPriceRequestDto { ConfirmedBy = "s" }));

        Assert.Contains(pid.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID03_NoPricingHistory_InvalidOperation()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var u = Guid.NewGuid();
        var lot = Lot(lotId, pid, u, UnitNav(u));

        var uow = BuildUow(lotId, lot, ProductNav(pid), null);
        var sut = MakeSut(uow.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ConfirmLotPriceAsync(lotId, new ConfirmPriceRequestDto { ConfirmedBy = "s" }));

        Assert.Contains("pricing suggestion first", ex.Message, StringComparison.OrdinalIgnoreCase);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID04_FinalPriceNull_UsesSuggested_AcceptedTrue_UnitSkipped()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId, suggested: 90m);

        var lot = Lot(lotId, pid, unitId, UnitNav(unitId));
        var product = ProductNav(pid);

        var unitRepo = new Mock<IGenericRepository<UnitOfMeasure>>();
        unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("unit repo should not load when Lot.Unit set"));

        var uow = BuildUow(lotId, lot, product, hist, unitRepo);
        var sut = MakeSut(uow.Object);

        var beforeUtc = DateTime.UtcNow.AddSeconds(-2);
        var dto = new ConfirmPriceRequestDto
        {
            FinalPrice = null,
            AcceptedSuggestion = true,
            PriceFeedback = null,
            ConfirmedBy = "staff-04",
        };

        var result = await sut.ConfirmLotPriceAsync(lotId, dto);

        Assert.Equal(ProductState.Priced, lot.Status);
        Assert.Equal(90m, lot.FinalUnitPrice);
        Assert.Equal(90m, lot.SuggestedUnitPrice);
        Assert.True(hist.AcceptedSuggestion);
        Assert.Null(hist.Feedback);
        Assert.Equal("staff-04", hist.ConfirmedBy);
        Assert.True(hist.ConfirmedAt.HasValue && hist.ConfirmedAt >= beforeUtc && hist.ConfirmedAt <= DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(90m, result.FinalPrice);
        Assert.Equal(90m, result.SuggestedPrice);
        Assert.Equal((float)hist.AIConfidence, result.PricingConfidence);

        unitRepo.Verify(
            r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()),
            Times.Never);

        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID05_FinalPriceExplicit_OverridesSuggested()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId, suggested: 100m);
        var lot = Lot(lotId, pid, unitId, UnitNav(unitId));

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist);
        var sut = MakeSut(uow.Object);

        var dto = new ConfirmPriceRequestDto
        {
            FinalPrice = 82.50m,
            AcceptedSuggestion = true,
            ConfirmedBy = "staff-05",
        };

        var result = await sut.ConfirmLotPriceAsync(lotId, dto);

        Assert.Equal(82.50m, lot.FinalUnitPrice);
        Assert.Equal(100m, lot.SuggestedUnitPrice);
        Assert.Equal(100m, hist.SuggestedPrice);
        Assert.Equal(82.50m, result.FinalPrice);
        Assert.Equal(100m, result.SuggestedPrice);
    }

    [Fact]
    public async Task UTCID06_AcceptedSuggestionFalse_Persisted()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId);
        var lot = Lot(lotId, pid, unitId, UnitNav(unitId));

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist);
        var sut = MakeSut(uow.Object);

        var dto = new ConfirmPriceRequestDto
        {
            FinalPrice = null,
            AcceptedSuggestion = false,
            ConfirmedBy = "staff-06",
        };

        await sut.ConfirmLotPriceAsync(lotId, dto);

        Assert.False(hist.AcceptedSuggestion);
    }

    [Fact]
    public async Task UTCID07_PriceFeedbackPersisted()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId);
        var lot = Lot(lotId, pid, unitId, UnitNav(unitId));

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist);
        var sut = MakeSut(uow.Object);

        var dto = new ConfirmPriceRequestDto
        {
            FinalPrice = null,
            AcceptedSuggestion = true,
            PriceFeedback = "Too high for shelf life",
            ConfirmedBy = "staff-07",
        };

        await sut.ConfirmLotPriceAsync(lotId, dto);

        Assert.Equal("Too high for shelf life", hist.Feedback);
    }

    [Fact]
    public async Task UTCID08_UnitIdEmpty_NoUnitRepositoryQuery()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var hist = HistoryRow(lotId);
        var lot = Lot(lotId, pid, Guid.Empty, navUnit: null);

        var unitRepo = new Mock<IGenericRepository<UnitOfMeasure>>();
        unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
            .ThrowsAsync(new InvalidOperationException("EnsureLotUnitLoaded must skip when UnitId empty"));

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist, unitRepo);
        var sut = MakeSut(uow.Object);

        var result = await sut.ConfirmLotPriceAsync(
            lotId,
            new ConfirmPriceRequestDto { ConfirmedBy = "staff-08" });

        Assert.Equal(ProductState.Priced, result.Status);
        unitRepo.Verify(
            r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()),
            Times.Never);
    }

    [Fact]
    public async Task UTCID09_UnitNavigationNull_LoadsUnitOfMeasure()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId);

        var loaded = UnitNav(unitId);
        var lot = Lot(lotId, pid, unitId, navUnit: null);

        var unitRepo = new Mock<IGenericRepository<UnitOfMeasure>>();
        unitRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()))
            .ReturnsAsync(loaded);

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist, unitRepo);
        var sut = MakeSut(uow.Object);

        var result = await sut.ConfirmLotPriceAsync(
            lotId,
            new ConfirmPriceRequestDto { ConfirmedBy = "staff-09" });

        Assert.Same(loaded, lot.Unit);
        Assert.Equal("Kg", result.UnitName);

        unitRepo.Verify(
            r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<UnitOfMeasure, bool>>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID10_CancellationTokenForwardedToSaveChanges()
    {
        var lotId = Guid.NewGuid();
        var pid = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var hist = HistoryRow(lotId);
        var lot = Lot(lotId, pid, unitId, UnitNav(unitId));

        var uow = BuildUow(lotId, lot, ProductNav(pid), hist);
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var sut = MakeSut(uow.Object);

        _ = await sut.ConfirmLotPriceAsync(
            lotId,
            new ConfirmPriceRequestDto { ConfirmedBy = "staff-10" },
            ct);

        uow.Verify(x => x.SaveChangesAsync(ct), Times.Once);
    }
}
