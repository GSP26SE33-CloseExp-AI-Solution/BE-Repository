namespace CloseExpAISolution.Domain.Enums;

public enum OrderState
{
    Pending,
    PaidProcessing,
    ReadyToShip,
    DeliveredWaitConfirm,
    Completed,
    Canceled,
    Refunded,
    Failed
}
