using System.Text.Json;
using CloseExpAISolution.Application.DTOs.Response;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;

namespace CloseExpAISolution.Application.Services.Fulfillment;

internal static class RefundDtoEnricher
{
    public static void EnrichRefundResponse(RefundResponseDto dto, Refund refund, Order order)
    {
        var itemIds = ParseRefundItemIds(refund.RefundedOrderItemIdsJson);
        dto.IsFullOrderRefund = itemIds is not { Count: > 0 };
        dto.Items = BuildRefundOrderItems(order, itemIds);
        dto.Steps = BuildProgressSteps(refund);
    }

    public static void ApplyOrderRefundDetails(OrderResponseDto orderDto, Order order, IReadOnlyList<Refund> refunds)
    {
        var activeRefunds = refunds
            .Where(r => r.Status != RefundState.Rejected)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        orderDto.Refunds = activeRefunds
            .Select(r =>
            {
                var dto = new RefundResponseDto
                {
                    RefundId = r.RefundId,
                    OrderId = r.OrderId,
                    TransactionId = r.TransactionId,
                    Amount = r.Amount,
                    Reason = r.Reason,
                    Status = r.Status.ToString(),
                    ProcessedBy = r.ProcessedBy,
                    ProcessedAt = r.ProcessedAt,
                    CreatedAt = r.CreatedAt,
                    RefundedOrderItemIds = ParseRefundItemIds(r.RefundedOrderItemIdsJson)
                };
                EnrichRefundResponse(dto, r, order);
                return dto;
            })
            .ToList();

        ApplyItemRefundProgress(orderDto.OrderItems, order.OrderItems, activeRefunds);
    }

    public static void ApplyAdminOrderRefundDetails(
        IList<AdminOrderListItemDto> orderDtos,
        IReadOnlyList<Order> orders,
        IReadOnlyList<Refund> refunds)
    {
        var refundsByOrder = refunds
            .GroupBy(r => r.OrderId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var orderDto in orderDtos)
        {
            if (!refundsByOrder.TryGetValue(orderDto.OrderId, out var orderRefunds))
                continue;

            var order = orders.First(o => o.OrderId == orderDto.OrderId);
            var activeRefunds = orderRefunds
                .Where(r => r.Status != RefundState.Rejected)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            var itemProgress = BuildItemRefundMap(order.OrderItems, activeRefunds);
            foreach (var itemDto in orderDto.OrderItems)
            {
                if (itemProgress.TryGetValue(itemDto.OrderItemId, out var refund))
                    itemDto.RefundProgress = ToItemProgress(refund, itemDto.TotalPrice);
            }
        }
    }

    public static void ApplyItemRefundProgress(
        IList<OrderItemResponseDto> itemDtos,
        ICollection<OrderItem> orderItems,
        IReadOnlyList<Refund> activeRefunds)
    {
        var itemProgress = BuildItemRefundMap(orderItems, activeRefunds);
        foreach (var itemDto in itemDtos)
        {
            if (itemProgress.TryGetValue(itemDto.OrderItemId, out var refund))
                itemDto.RefundProgress = ToItemProgress(refund, itemDto.LineTotal);
        }
    }

    public static IReadOnlyList<Guid>? ParseRefundItemIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var list = JsonSerializer.Deserialize<List<Guid>>(json);
            return list is { Count: > 0 } ? list : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<Guid, Refund> BuildItemRefundMap(
        ICollection<OrderItem> orderItems,
        IReadOnlyList<Refund> activeRefunds)
    {
        var map = new Dictionary<Guid, Refund>();
        foreach (var refund in activeRefunds)
        {
            var ids = ParseRefundItemIds(refund.RefundedOrderItemIdsJson);
            if (ids is { Count: > 0 })
            {
                foreach (var id in ids)
                {
                    if (!map.ContainsKey(id))
                        map[id] = refund;
                }
            }
            else
            {
                foreach (var item in orderItems)
                {
                    if (!map.ContainsKey(item.OrderItemId))
                        map[item.OrderItemId] = refund;
                }
            }
        }

        return map;
    }

    private static IReadOnlyList<RefundOrderItemDto> BuildRefundOrderItems(Order order, IReadOnlyList<Guid>? itemIds)
    {
        var items = itemIds is { Count: > 0 }
            ? order.OrderItems.Where(oi => itemIds.Contains(oi.OrderItemId))
            : order.OrderItems;

        return items.Select(MapRefundOrderItem).ToList();
    }

    private static RefundOrderItemDto MapRefundOrderItem(OrderItem item) =>
        new()
        {
            OrderItemId = item.OrderItemId,
            ProductName = item.StockLot?.Product?.Name,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            PackagingStatus = item.PackagingStatus.ToString(),
            DeliveryStatus = item.DeliveryStatus?.ToString()
        };

    private static OrderItemRefundProgressDto ToItemProgress(Refund refund, decimal lineAmount)
    {
        var itemIds = ParseRefundItemIds(refund.RefundedOrderItemIdsJson);
        return new OrderItemRefundProgressDto
        {
            RefundId = refund.RefundId,
            RefundStatus = refund.Status.ToString(),
            LineRefundAmount = lineAmount,
            Reason = refund.Reason,
            CreatedAt = refund.CreatedAt,
            ProcessedAt = refund.ProcessedAt,
            ProcessedBy = refund.ProcessedBy,
            IsFullOrderRefund = itemIds is not { Count: > 0 },
            Steps = BuildProgressSteps(refund)
        };
    }

    public static IReadOnlyList<RefundProgressStepDto> BuildProgressSteps(Refund refund)
    {
        if (refund.Status == RefundState.Rejected)
        {
            return new[]
            {
                new RefundProgressStepDto
                {
                    Step = "Pending",
                    IsCompleted = true,
                    IsCurrent = false,
                    OccurredAt = refund.CreatedAt
                },
                new RefundProgressStepDto
                {
                    Step = "Rejected",
                    IsCompleted = true,
                    IsCurrent = true,
                    OccurredAt = refund.ProcessedAt
                }
            };
        }

        var approved = refund.Status is RefundState.Approved or RefundState.Completed;
        var completed = refund.Status == RefundState.Completed;

        return new[]
        {
            new RefundProgressStepDto
            {
                Step = "Pending",
                IsCompleted = true,
                IsCurrent = refund.Status == RefundState.Pending,
                OccurredAt = refund.CreatedAt
            },
            new RefundProgressStepDto
            {
                Step = "Approved",
                IsCompleted = approved,
                IsCurrent = refund.Status == RefundState.Approved,
                OccurredAt = approved ? refund.ProcessedAt : null
            },
            new RefundProgressStepDto
            {
                Step = "Completed",
                IsCompleted = completed,
                IsCurrent = refund.Status == RefundState.Completed,
                OccurredAt = completed ? refund.ProcessedAt : null
            }
        };
    }
}
