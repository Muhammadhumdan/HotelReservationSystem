using System.Globalization;

namespace HotelReservation.Client.Services;

/// <summary>Tiny helpers so money and dates look the same on every page.</summary>
public static class Fmt
{
    public static string Money(decimal amount) => $"PKR {amount.ToString("N0", CultureInfo.InvariantCulture)}";
    public static string Date(DateOnly date) => date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
}
