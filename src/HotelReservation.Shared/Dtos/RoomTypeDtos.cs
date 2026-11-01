using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Shared.Dtos;

/// <summary>What the API sends back about a room type.</summary>
public class RoomTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal PricePerNight { get; set; }
    public int Capacity { get; set; }
    public int RoomCount { get; set; }
}

/// <summary>What the client sends to create/update a room type. The attributes are the VALIDATION rules.</summary>
public class RoomTypeRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(50, ErrorMessage = "Name can be at most 50 characters.")]
    public string Name { get; set; } = "";

    [StringLength(300, ErrorMessage = "Description can be at most 300 characters.")]
    public string? Description { get; set; }

    [Range(1, 1000000, ErrorMessage = "Price per night must be between 1 and 1,000,000.")]
    public decimal PricePerNight { get; set; }

    [Range(1, 10, ErrorMessage = "Capacity must be between 1 and 10 guests.")]
    public int Capacity { get; set; } = 2;
}
