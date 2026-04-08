namespace CloseExpAISolution.Application.Services.Fulfillment;

public static class RefundAmountCalculator
{
    public static decimal ComputeRefundable(decimal paidAmount, decimal alreadyRefunded)
    {
        var refundable = paidAmount - alreadyRefunded;
        return refundable > 0 ? refundable : 0;
    }
}

