using System.ComponentModel.DataAnnotations;
using HotelReservation.Shared.Enums;

namespace HotelReservation.Shared.Dtos;

public class ReservationDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int Guests { get; set; }
    public decimal TotalPrice { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Calculated, not stored: number of nights = check-out day minus check-in day.</summary>
    public int Nights => CheckOutDate.DayNumber - CheckInDate.DayNumber;
}

/// <summary>
/// Request to make a booking. IValidatableObject lets us write rules that compare TWO fields
/// (e.g. check-out must be after check-in), which simple attributes cannot do.
/// </summary>
public class ReservationRequest : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Please choose a customer.")]
    public int CustomerId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a room.")]
    public int RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }

    [Range(1, 10, ErrorMessage = "Guests must be between 1 and 10.")]
    public int Guests { get; set; } = 1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOutDate <= CheckInDate)
            yield return new ValidationResult("Check-out date must be after the check-in date.",
                new[] { nameof(CheckOutDate) });

        if (CheckInDate < DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("Check-in date cannot be in the past.",
                new[] { nameof(CheckInDate) });

        if ((CheckOutDate.DayNumber - CheckInDate.DayNumber) > 30)
            yield return new ValidationResult("A single booking can be at most 30 nights.",
                new[] { nameof(CheckOutDate) });
    }
}

public class ChangeStatusRequest
{
    public ReservationStatus Status { get; set; }
}
