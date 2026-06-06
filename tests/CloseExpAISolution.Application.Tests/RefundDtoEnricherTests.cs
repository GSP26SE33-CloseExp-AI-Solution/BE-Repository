using CloseExpAISolution.Application.Services.Fulfillment;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class RefundDtoEnricherTests
{
    [Fact]
    public void ApplyItemRefundProgress_LinksPartialRefundToFailedLineOnly()
    {
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderItems =
            [
                new OrderItem { OrderItemId = item1Id, Quantity = 1, UnitPrice = 50_000, TotalPrice = 50_000 },
                new OrderItem { OrderItemId = item2Id, Quantity = 1, UnitPrice = 30_000, TotalPrice = 30_000 }
            ]
        };

        var refund = new Refund
        {
            RefundId = refundId,
            OrderId = order.OrderId,
            Amount = 50_000,
            Status = RefundState.Pending,
            CreatedAt = DateTime.UtcNow,
            RefundedOrderItemIdsJson = $"[\"{item1Id}\"]"
        };

        var itemDtos = new List<Application.DTOs.Response.OrderItemResponseDto>
        {
            new() { OrderItemId = item1Id, TotalPrice = 50_000 },
            new() { OrderItemId = item2Id, TotalPrice = 30_000 }
        };

        RefundDtoEnricher.ApplyItemRefundProgress(itemDtos, order, new[] { refund });

        var progress = itemDtos[0].RefundProgress;
        Assert.NotNull(progress);
        Assert.Equal(refundId, progress.RefundId);
        Assert.Equal("Pending", progress.RefundStatus);
        Assert.Equal(3, progress.Steps.Count);
        Assert.Null(itemDtos[1].RefundProgress);
    }

    [Fact]
    public void BuildProgressSteps_CompletedRefund_MarksAllStepsCompleted()
    {
        var refund = new Refund
        {
            Status = RefundState.Completed,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ProcessedAt = DateTime.UtcNow
        };

        var steps = RefundDtoEnricher.BuildProgressSteps(refund);

        Assert.Equal(3, steps.Count);
        Assert.True(steps.All(s => s.IsCompleted));
        Assert.Equal("Completed", steps.Last().Step);
    }
}
