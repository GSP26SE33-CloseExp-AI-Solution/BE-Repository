using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloseExpAISolution.Domain.Enums
{
    // ===== For models state =====
    // For order state
    public enum OrderState
    {
        Pending, // waiting for payment
        Paid_Processing, // paid and process and waiting for packing
        Ready_To_Ship, // ready to ship
        Delivered_Wait_Confirm, // delivered and waiting for confirm by Food Vendors
        Completed, // confirmed by Food Vendors

        // Before Pending only
        Canceled,
        Refunded,
        Failed
    }

    // For product state
    public enum ProductState
    {
       Verified, // product is verified by Supplier
       Priced, // product is priced by Supplier
       Expired, // permanent expired
       Deleted, // permanent delete
       Hidden // for save draft product
    }

    // For user state
    public enum UserState
    {
        Active, // real time active
        Inactive, // real time un active
        Banned, // permanent ban
        Deleted, // permanent delete
        Hidden // permanent hide
    }

    // For review/feedback state
    public enum ReviewState
    {
        Pending,
        Approved, // approved by Admin
        Rejected // rejected by Admin
    }

    // For rating points
    public enum RatingPoints
    {
        OneStar = 1,
        TwoStar = 2,
        ThreeStar = 3,
        FourStar = 4,
        FiveStar = 5
    }

    // ==== For things work in system ====
    // For notification state
    public enum NotificationState
    {
        Pending,
        Sent,
        Failed
    }

    // ===== For third party service state =====
    // For payment state
    public enum PaymentState
    {
        Pending,
        Paid,
        Failed
    }

    // For upload state
    public enum UploadState // SHould change the value when implement cloud storage
    {
        Pending,
        Uploaded,
        Failed
    }

    // For AI state
    public enum AIState
    {
        Pending,
        Processed,
        Failed
    }
}
