namespace HotelReservation.Api.Models;

/// <summary>A category of room (Standard, Deluxe, Suite...). One RoomType has MANY Rooms.</summary>
public class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }

    // Navigation property: EF Core fills this with all rooms of this type (one-to-many).
    public List<Room> Rooms { get; set; } = new();
}
