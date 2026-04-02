namespace CloseExpAISolution.Domain.Enums;

public enum OrderState
{
    Pending,
    Paid,
    ReadyToShip,
    DeliveredWaitConfirm,
    Completed,
    Canceled,
    Refunded,
    Failed
}
