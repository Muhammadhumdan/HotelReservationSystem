namespace HotelReservation.Api.Models;

/// <summary>A physical room. Belongs to ONE RoomType, and can have MANY Reservations over time.</summary>
public class Room
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = "";
    public int Floor { get; set; }

    // Foreign key + navigation: the link to RoomTypes table.
    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    /// <summary>false = under maintenance / closed. Such rooms cannot be booked.</summary>
    public bool IsActive { get; set; } = true;

    public List<Reservation> Reservations { get; set; } = new();
}
