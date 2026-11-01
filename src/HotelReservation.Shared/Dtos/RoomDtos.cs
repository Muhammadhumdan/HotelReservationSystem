using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Shared.Dtos;

/// <summary>A room as shown in the admin list.</summary>
public class RoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = "";
    public int Floor { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = "";
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Calculated automatically from reservations: is someone staying/booked in this room today?</summary>
    public bool IsOccupiedToday { get; set; }
}

public class RoomRequest
{
    [Required(ErrorMessage = "Room number is required.")]
    [StringLength(10, ErrorMessage = "Room number can be at most 10 characters.")]
    public string RoomNumber { get; set; } = "";

    [Range(0, 100, ErrorMessage = "Floor must be between 0 and 100.")]
    public int Floor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a room type.")]
    public int RoomTypeId { get; set; }

    /// <summary>Untick when the room is under maintenance - it will not appear in customer searches.</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>One search result: a room that is free for the whole requested stay, with the price worked out.</summary>
public class AvailableRoomDto
{
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public int Floor { get; set; }
    public string RoomTypeName { get; set; } = "";
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public decimal PricePerNight { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
}
