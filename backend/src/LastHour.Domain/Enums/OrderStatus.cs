namespace LastHour.Domain.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Ready = 3,
    PickedUp = 4,
    Delivered = 5,
    Cancelled = 6,
    Rejected = 7
}
