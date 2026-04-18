namespace CloseExpAISolution.Application;

public static class OrderTotalsHelper
{
    public static decimal ComputeFinalAmount(
        decimal totalAmount,
        decimal discountAmount,
        decimal deliveryFee,
        decimal systemUsageFeeAmount) =>
        Math.Max(0, totalAmount - discountAmount) + deliveryFee + systemUsageFeeAmount;
}
