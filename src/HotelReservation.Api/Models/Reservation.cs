using HotelReservation.Shared.Enums;

namespace HotelReservation.Api.Models;

/// <summary>
/// A booking = ONE customer + ONE room + a date range.
/// Stay is [CheckInDate, CheckOutDate): the guest sleeps the nights from check-in up to (not including)
/// the check-out day. So one guest can check out on the 5th and another can check in on the 5th.
/// </summary>
public class Reservation
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
