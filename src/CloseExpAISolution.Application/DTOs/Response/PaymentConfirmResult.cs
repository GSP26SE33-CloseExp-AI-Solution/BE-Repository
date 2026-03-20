namespace CloseExpAISolution.Application.DTOs.Response;

/// <summary>Outcome of polling PayOS and updating local <c>Transaction</c> / order.</summary>
public sealed class PaymentConfirmResult
{
    public bool Success { get; init; }

    /// <summary>Application-level reason; maps to HTTP status in the API layer.</summary>
    public PaymentConfirmErrorCode ErrorCode { get; init; }

    public string Message { get; init; } = string.Empty;

    /// <summary>PayOS payment-link status string, for troubleshooting.</summary>
    public string? PayOsStatus { get; init; }

    public long? AmountPaid { get; init; }
    public long? Amount { get; init; }

    public static PaymentConfirmResult Ok() => new() { Success = true };

    public static PaymentConfirmResult PayOsFailure(string message) => new()
    {
        Success = false,
        ErrorCode = PaymentConfirmErrorCode.PayOsUnavailable,
        Message = message
    };

    public static PaymentConfirmResult NotPaidYet(
        string payOsStatus,
        long amountPaid,
        long amount,
        string? extra = null) => new()
    {
        Success = false,
        ErrorCode = PaymentConfirmErrorCode.PaymentNotComplete,
        Message = string.IsNullOrEmpty(extra)
            ? $"PayOS reports status '{payOsStatus}' (AmountPaid={amountPaid}, Amount={amount})."
            : extra!,
        PayOsStatus = payOsStatus,
        AmountPaid = amountPaid,
        Amount = amount
    };

    public static PaymentConfirmResult MissingTransaction(long payOsOrderCode) => new()
    {
        Success = false,
        ErrorCode = PaymentConfirmErrorCode.TransactionMissing,
        Message = $"No local transaction for PayOS orderCode {payOsOrderCode}. " +
                  "Use the code from the payment link created by this API (see Transactions.PayOSOrderCode in the database)."
    };
}

public enum PaymentConfirmErrorCode
{
    None = 0,
    /// <summary>Could not call PayOS or unexpected error from SDK.</summary>
    PayOsUnavailable,
    /// <summary>PayOS link exists but is not settled as paid yet.</summary>
    PaymentNotComplete,
    /// <summary>No matching <c>Transaction</c> row for this PayOS order code.</summary>
    TransactionMissing
}
