namespace CloseExpAISolution.Domain.Enums;

public enum RefundEmailOutboxStatus : byte
{
    Pending = 0,
    Sent = 1,
    DeadLetter = 2
}
