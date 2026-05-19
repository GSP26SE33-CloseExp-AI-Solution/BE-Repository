using System.Linq.Expressions;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.AIService.Models;
using CloseExpAISolution.Application.Services;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Application.Services.Interface;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Infrastructure.Repositories.Interface;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

/// <summary>
/// FN007 — UTCID01–UTCID13 per <c>.github/instructions/analyze-product-image-async-test-sheet.md</c>
/// (<see cref="ProductWorkflowService.AnalyzeProductImageAsync"/>).
/// Run: <c>dotnet test --filter "FullyQualifiedName~ProductWorkflowServiceAnalyzeProductImageAsyncTests"</c>
/// </summary>
public sealed class ProductWorkflowServiceAnalyzeProductImageAsyncTests
{
    private const string PublicImageUrl = "https://bucket.example/pub/photo.jpg";

    private sealed class FakeUploadTarget
    {
        public string Url { get; set; } = "";
    }

    private sealed class CaptureUploadArgs
    {
        public string? SeenFileName;
        public string? SeenContentType;
        public CancellationToken SeenCt;

        public void Observe(string fn, string ct, CancellationToken ctk)
        {
            SeenFileName = fn;
            SeenContentType = ct;
            SeenCt = ctk;
        }
    }

    private static MemoryStream EmptyStream => new(Array.Empty<byte>());

    private static Mock<IUnitOfWork> CreateUow(Guid supermarketId, Supermarket? supermarket)
    {
        var uow = new Mock<IUnitOfWork>();
        var smRepo = new Mock<ISupermarketRepository>();
        smRepo
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Supermarket, bool>>>()))
            .ReturnsAsync(supermarket);
        uow.SetupGet(x => x.SupermarketRepository).Returns(smRepo.Object);
        return uow;
    }

    private static Mock<IR2StorageService> CreateR2(
        Func<Stream?, string?, string?, CancellationToken, object>? uploadFactory = null,
        CaptureUploadArgs? capture = null)
    {
        var r2 = new Mock<IR2StorageService>();
        r2.Setup(r => r.UploadToR2Async(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream _, string fn, string ct, CancellationToken ctk) =>
            {
                capture?.Observe(fn, ct, ctk);
                uploadFactory ??= (_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl };
                return uploadFactory(_, fn, ct, ctk)!;
            });

        return r2;
    }

    private static void StubPreSigned(Mock<IR2StorageService> r2, string? value)
    {
        r2.Setup(r => r.GetPreSignedUrlForImage(It.IsAny<string>(), It.IsAny<TimeSpan?>())).Returns(value);
    }

    private static ProductWorkflowService MakeSut(
        IUnitOfWork uow,
        Mock<IServiceProvider> serviceProviderMock,
        IR2StorageService r2,
        IAIServiceClient ai,
        IBarcodeLookupService lookup,
        ILogger<ProductWorkflowService> logger)
    {
        serviceProviderMock.Setup(s => s.GetService(typeof(IR2StorageService))).Returns(r2);

        return new ProductWorkflowService(
            uow,
            ai,
            serviceProviderMock.Object,
            Mock.Of<IMarketPriceService>(),
            lookup,
            logger);
    }

    [Fact]
    public async Task UTCID01_SupermarketMissing_ArgumentException()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUow(smId, null);
        var r2 = CreateR2();
        StubPreSigned(r2, "/pre/");
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(
            uow.Object,
            sp,
            r2.Object,
            Mock.Of<IAIServiceClient>(),
            Mock.Of<IBarcodeLookupService>(l =>
                l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.FromResult<BarcodeProductInfo?>(null)),
            Mock.Of<ILogger<ProductWorkflowService>>());

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.AnalyzeProductImageAsync(smId, EmptyStream, "x.png", "image/png"));

        Assert.Equal("supermarketId", ex.ParamName);
        Assert.Contains(smId.ToString(), ex.Message, StringComparison.Ordinal);
        r2.Verify(
            x => x.UploadToR2Async(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UTCID02_GetService_ReturnsNull_InvalidOperationBeforeUpload()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });

        var r2Ghost = Mock.Of<IR2StorageService>();

        var sp = new Mock<IServiceProvider>();
        sp.Setup(s => s.GetService(typeof(IR2StorageService))).Returns((IR2StorageService?)null);

        var sut = new ProductWorkflowService(
            uow.Object,
            Mock.Of<IAIServiceClient>(),
            sp.Object,
            Mock.Of<IMarketPriceService>(),
            Mock.Of<IBarcodeLookupService>(),
            Mock.Of<ILogger<ProductWorkflowService>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeProductImageAsync(smId, EmptyStream, "x.png", "image/png"));

        Assert.Contains("R2 storage service is not configured", ex.Message, StringComparison.OrdinalIgnoreCase);
        Mock.Get(r2Ghost).Verify(
            x => x.UploadToR2Async(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Ghost R2 mock must remain unused.");
    }

    [Fact]
    public async Task UTCID03_UploadUrlBlank_InvalidOperation()
    {
        var smId = Guid.NewGuid();
        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = "" });
        StubPreSigned(r2, "/pre/");
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(
            uow.Object,
            sp,
            r2.Object,
            Mock.Of<IAIServiceClient>(),
            Mock.Of<IBarcodeLookupService>(),
            Mock.Of<ILogger<ProductWorkflowService>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.AnalyzeProductImageAsync(smId, EmptyStream, "x.png", "image/png"));

        Assert.Contains("R2", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UTCID04_PreSignedEmpty_UsesUploadedUrlForExtract()
    {
        var smId = Guid.NewGuid();
        var myUrl = "https://stored.example/a.jpg";

        string? extractedUrlArg = null;
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((u, _) => extractedUrlArg = u)
            .ReturnsAsync((OcrResponse?)null);

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = myUrl });
        StubPreSigned(r2, null);
        StubPreSigned(r2, "");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });

        var sp = new Mock<IServiceProvider>();

        var lookup = Mock.Of<IBarcodeLookupService>(l =>
            l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.FromResult<BarcodeProductInfo?>(null));

        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup, Mock.Of<ILogger<ProductWorkflowService>>());

        _ = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "fn.jpg", "image/jpeg");

        Assert.Equal(myUrl, extractedUrlArg);
        r2.Verify(x => x.GetPreSignedUrlForImage(myUrl, It.IsAny<TimeSpan?>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task UTCID05_SkipAi_NoExtract_ImageUrlReturned()
    {
        var smId = Guid.NewGuid();

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "presigned/here");

        var ai = new Mock<IAIServiceClient>();
        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(
            uow.Object,
            sp,
            r2.Object,
            ai.Object,
            Mock.Of<IBarcodeLookupService>(),
            Mock.Of<ILogger<ProductWorkflowService>>());

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png", skipAi: true);

        Assert.True(result.AiSkipped);
        Assert.Equal(PublicImageUrl, result.ImageUrl);
        ai.Verify(x => x.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID06_ExtractNull_ResponseDefaultsUnchanged()
    {
        var smId = Guid.NewGuid();
        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "https://presigned/");
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((OcrResponse?)null);
        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(
            uow.Object,
            sp,
            r2.Object,
            ai.Object,
            Mock.Of<IBarcodeLookupService>(),
            Mock.Of<ILogger<ProductWorkflowService>>());

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.False(result.AiSkipped);
        Assert.Null(result.RawOcrData);
        Assert.Equal(0, result.Confidence);
        Assert.Null(result.ExtractedInfo.Name);
        Assert.Null(result.BarcodeLookupInfo);
    }

    [Fact]
    public async Task UTCID07_ExtractThrows_PlaceholderName_LogErrorVerified()
    {
        var smId = Guid.NewGuid();
        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "https://ps/");
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("ocr down"));

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<ProductWorkflowService>>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, Mock.Of<IBarcodeLookupService>(), logger.Object);

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.Equal("OCR Error - Manual Entry Required", result.ExtractedInfo.Name);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Error calling AI OCR", StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID08_RichOcrMapped_NoBarcode_LookupNever()
    {
        var smId = Guid.NewGuid();

        var exp = DateTime.UtcNow.Date.AddMonths(6);
        var mfd = DateTime.UtcNow.Date.AddMonths(-3);

        var ocr = new OcrResponse
        {
            Confidence = 0.91f,
            ProductInfo = new ProductInfo
            {
                Name = "OCR Name",
                Brand = "B",
                Barcode = null,
                Ingredients = ["a", "b"],
                Manufacturer = new ManufacturerInfo { Name = "Corp" },
                Origin = "VN",
                Weight = "500g",
                DetectedCategory = new CategoryInfo { Name = "Dairy", Confidence = 1 },
                NutritionFacts = new Dictionary<string, object> { ["Energy"] = 100m }
            },
            ExpiryDate = new DateInfo { Value = exp },
            ManufacturedDate = new DateInfo { Value = mfd }
        };

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ocr);

        var lookup = new Mock<IBarcodeLookupService>();

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "https://ps/");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, Mock.Of<ILogger<ProductWorkflowService>>());

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.Equal("OCR Name", result.ExtractedInfo.Name);
        Assert.Equal("B", result.ExtractedInfo.Brand);
        Assert.Null(result.ExtractedInfo.Barcode);
        Assert.Equal("Dairy", result.ExtractedInfo.Category);
        Assert.Equal(exp, result.ExtractedInfo.ExpiryDate);
        Assert.Equal(mfd, result.ExtractedInfo.ManufactureDate);
        Assert.Equal("500g", result.ExtractedInfo.Weight);
        Assert.Equal("Corp", result.ExtractedInfo.Manufacturer);
        Assert.Equal("VN", result.ExtractedInfo.Origin);
        Assert.NotNull(result.ExtractedInfo.NutritionFacts);
        Assert.True(result.ExtractedInfo.NutritionFacts!.TryGetValue("Energy", out var v) && v == "100");
        Assert.NotNull(result.RawOcrData);
        Assert.False(string.IsNullOrWhiteSpace(result.RawOcrData));
        lookup.Verify(l => l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID09_BarcodeLookupReturnsDto_MapBarcodeLookupInfo()
    {
        var smId = Guid.NewGuid();
        var ocr = new OcrResponse { Confidence = 0.8f, ProductInfo = new ProductInfo { Name = "P", Barcode = "8931" } };

        var lookupDto = new BarcodeProductInfo
        {
            Barcode = "8931",
            ProductName = "ExtProd",
            Brand = "Eb",
            Category = "Ct",
            Source = "tst"
        };

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ocr);

        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupAsync("8931", It.IsAny<CancellationToken>())).ReturnsAsync(lookupDto);

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "presigned");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, Mock.Of<ILogger<ProductWorkflowService>>());

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.NotNull(result.BarcodeLookupInfo);
        Assert.Equal("8931", result.BarcodeLookupInfo!.Barcode);
        Assert.Equal("ExtProd", result.BarcodeLookupInfo.ProductName);
        Assert.Equal("Eb", result.BarcodeLookupInfo.Brand);
    }

    [Fact]
    public async Task UTCID10_BarcodeLookupNull_NoBarcodeLookupInfo()
    {
        var smId = Guid.NewGuid();
        var ocr = new OcrResponse { Confidence = 0.5f, ProductInfo = new ProductInfo { Barcode = "8931" } };

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ocr);

        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupAsync("8931", It.IsAny<CancellationToken>())).ReturnsAsync(() => null);

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "p");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, Mock.Of<ILogger<ProductWorkflowService>>());

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.Null(result.BarcodeLookupInfo);
        lookup.Verify(l => l.LookupAsync("8931", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UTCID11_LookupThrows_LogWarning_NoOutwardThrow()
    {
        var smId = Guid.NewGuid();
        var ocr = new OcrResponse { Confidence = 1f, ProductInfo = new ProductInfo { Name = "Pn", Barcode = "8931" } };

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ocr);

        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupAsync("8931", It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("fail"));

        var logger = new Mock<ILogger<ProductWorkflowService>>();

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "pre");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, logger.Object);

        var result = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        Assert.Null(result.BarcodeLookupInfo);
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Error looking up barcode", StringComparison.Ordinal)),
                It.IsAny<IOException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UTCID12_EmptyBarcode_LookupNotInvoked()
    {
        var smId = Guid.NewGuid();

        var ocr = new OcrResponse { Confidence = 0.7f, ProductInfo = new ProductInfo { Name = "Pn", Barcode = "" } };

        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(ocr);

        var lookup = new Mock<IBarcodeLookupService>();

        var r2 = CreateR2((_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl });
        StubPreSigned(r2, "p");

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, Mock.Of<ILogger<ProductWorkflowService>>());

        _ = await sut.AnalyzeProductImageAsync(smId, EmptyStream, "z.png", "image/png");

        lookup.Verify(l => l.LookupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UTCID13_FileNameContentTypeAndTokenForwardedToUploadOcrLookup()
    {
        var smId = Guid.NewGuid();
        await using var body = new MemoryStream(new byte[] { 9, 8, 7 });

        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        var barcode = "999";
        var capture = new CaptureUploadArgs();

        var r2 = CreateR2(
            (_, _, _, _) => new FakeUploadTarget { Url = PublicImageUrl },
            capture);

        StubPreSigned(r2, $"pre:{PublicImageUrl}");

        string? aiUrl = null;
        var aiCapturedCt = CancellationToken.None;
        var ai = new Mock<IAIServiceClient>();
        ai.Setup(a => a.ExtractFromUrlAsync(It.IsAny<string>(), ct))
            .Callback<string, CancellationToken>((url, tk) =>
            {
                aiUrl = url;
                aiCapturedCt = tk;
            })
            .ReturnsAsync(new OcrResponse
            {
                Confidence = 0.6f,
                ProductInfo = new ProductInfo { Barcode = barcode, Name = "Prod" },
            });

        var lookupCapturedCt = CancellationToken.None;
        var lookup = new Mock<IBarcodeLookupService>();
        lookup.Setup(l => l.LookupAsync(barcode, ct))
            .Callback<string, CancellationToken>((_, tk) => lookupCapturedCt = tk)
            .ReturnsAsync(() => null);

        var uow = CreateUow(smId, new Supermarket { SupermarketId = smId, Name = "S" });
        var sp = new Mock<IServiceProvider>();
        var sut = MakeSut(uow.Object, sp, r2.Object, ai.Object, lookup.Object, Mock.Of<ILogger<ProductWorkflowService>>());

        const string fname = "my-prod.webp";
        const string ctype = "image/webp";
        await sut.AnalyzeProductImageAsync(smId, body, fname, ctype, cancellationToken: ct);

        Assert.Equal(fname, capture.SeenFileName);
        Assert.Equal(ctype, capture.SeenContentType);
        Assert.Equal(ct, capture.SeenCt);
        Assert.Equal($"pre:{PublicImageUrl}", aiUrl);
        Assert.Equal(ct, aiCapturedCt);
        Assert.Equal(ct, lookupCapturedCt);

        ai.Verify(a => a.ExtractFromUrlAsync(It.IsAny<string>(), ct), Times.Once);
        lookup.Verify(l => l.LookupAsync(barcode, ct), Times.Once);
        r2.Verify(r =>
                r.UploadToR2Async(body, fname, ctype, ct),
            Times.Once);
    }
}
