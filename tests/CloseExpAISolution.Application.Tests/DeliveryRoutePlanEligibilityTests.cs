using CloseExpAISolution.Application.Services.Class;
using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
using Xunit;

namespace CloseExpAISolution.Application.Tests;

public class DeliveryRoutePlanEligibilityTests
{
   [Fact]
   public void HasRoutableItemsForRoute_PartialDeliveredOrder_ReturnsTrue()
   {
      var items = new[]
      {
            new OrderItem
            {
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = DeliveryState.DeliveredWaitConfirm
            },
            new OrderItem
            {
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = DeliveryState.ReadyToShip
            }
        };

      var result = DeliveryService.HasRoutableItemsForRoute(items);

      Assert.True(result);
   }

   [Fact]
   public void HasRoutableItemsForRoute_AllTerminal_ReturnsFalse()
   {
      var items = new[]
      {
            new OrderItem
            {
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = DeliveryState.DeliveredWaitConfirm
            },
            new OrderItem
            {
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = DeliveryState.Completed
            },
            new OrderItem
            {
                PackagingStatus = PackagingState.Completed,
                DeliveryStatus = DeliveryState.Failed
            }
        };

      var result = DeliveryService.HasRoutableItemsForRoute(items);

      Assert.False(result);
   }
}
