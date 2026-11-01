namespace HotelReservation.Api.Models;

/// <summary>A guest. One Customer can have MANY Reservations (their booking history).</summary>
public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Reservation> Reservations { get; set; } = new();
}
