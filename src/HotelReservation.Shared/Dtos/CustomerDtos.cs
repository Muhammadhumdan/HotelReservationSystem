using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Shared.Dtos;

public class CustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CustomerRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be 3 to 100 characters.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\+?[0-9\- ]{7,20}$", ErrorMessage = "Phone must be 7-20 digits (spaces, - and a leading + are allowed).")]
    public string Phone { get; set; } = "";
}
