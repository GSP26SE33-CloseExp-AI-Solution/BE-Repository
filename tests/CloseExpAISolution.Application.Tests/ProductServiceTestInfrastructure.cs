using AutoMapper;
using CloseExpAISolution.Application.AIService.Interfaces;
using CloseExpAISolution.Application.Mappings;
using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Infrastructure.Context;
using CloseExpAISolution.Infrastructure.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CloseExpAISolution.Application.Tests;

/// <summary>Shared SQLite + Mapper setup for ProductService unit tests.</summary>
internal static class ProductServiceTestInfrastructure
{
    private static readonly MapperConfiguration MapperConfig = BuildMapper();

    internal static IMapper CreateMapper() => MapperConfig.CreateMapper();

    private static MapperConfiguration BuildMapper() =>
        new(cfg => cfg.AddProfile<ProductMappingProfile>(), NullLoggerFactory.Instance);

    internal static IAIServiceClient LooseAiClient() =>
        new Mock<IAIServiceClient>(MockBehavior.Loose).Object;

    internal static ProductService CreateProductService(
        ApplicationDbContext ctx,
        IMapper? mapper = null,
        IAIServiceClient? ai = null)
    {
        mapper ??= CreateMapper();
        var uow = new UnitOfWork(ctx);
        return new ProductService(uow, ctx, mapper, ai ?? LooseAiClient(), NullLogger<ProductService>.Instance);
    }
}
