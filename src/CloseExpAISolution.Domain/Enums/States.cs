using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloseExpAISolution.Domain.Enums
{
    public enum OrderState
    {
        Pending,
        PaidProcessing,
        ReadyToShip,
        DeliveredWaitingForConfirm,
        Completed,
        Canceled,
        Refunded,
        Failed
    }

    public enum ProductState
    {
       Verified,
       Priced,
       OutOfStock,
       Available
    }

    public enum UserState
    {
        Active,
        Inactive,
        Banned
    }
}
