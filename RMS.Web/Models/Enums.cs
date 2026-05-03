namespace RMS.Web.Models
{
    public enum OrderType
    {
        Delivery,
        Pickup
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparing,
        Ready,
        ReadyForPickup,
        OutForDelivery,
        Delivered,
        PickedUp,
        Cancelled
    }
}
