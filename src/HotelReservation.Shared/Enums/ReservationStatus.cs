namespace HotelReservation.Shared.Enums;

/// <summary>
/// The life of a booking, like a parcel being tracked:
/// Confirmed -> CheckedIn -> CheckedOut   (or Confirmed -> Cancelled)
/// Only Confirmed and CheckedIn bookings "block" a room from being booked by someone else.
/// </summary>
public enum ReservationStatus
{
    Confirmed = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Cancelled = 4
}
