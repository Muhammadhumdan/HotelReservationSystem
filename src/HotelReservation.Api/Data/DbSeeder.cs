using HotelReservation.Api.Models;
using HotelReservation.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

/// <summary>Fills an EMPTY database with sample hotel data so you can test immediately.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(HotelDbContext db)
    {
        if (await db.RoomTypes.AnyAsync()) return;   // already has data -> do nothing

        var standard = new RoomType { Name = "Standard Single", Description = "Cosy room with one bed, AC and Wi-Fi.", PricePerNight = 8000, Capacity = 1 };
        var deluxe = new RoomType { Name = "Deluxe Double", Description = "Spacious room with a king bed and city view.", PricePerNight = 14000, Capacity = 2 };
        var family = new RoomType { Name = "Family Suite", Description = "Two bedrooms and a lounge, good for families.", PricePerNight = 22000, Capacity = 4 };
        var executive = new RoomType { Name = "Executive Suite", Description = "Top floor suite with a work desk and breakfast.", PricePerNight = 35000, Capacity = 3 };
        db.RoomTypes.AddRange(standard, deluxe, family, executive);

        var rooms = new List<Room>
        {
            new() { RoomNumber = "101", Floor = 1, RoomType = standard },
            new() { RoomNumber = "102", Floor = 1, RoomType = standard },
            new() { RoomNumber = "103", Floor = 1, RoomType = standard },
            new() { RoomNumber = "201", Floor = 2, RoomType = deluxe },
            new() { RoomNumber = "202", Floor = 2, RoomType = deluxe },
            new() { RoomNumber = "203", Floor = 2, RoomType = deluxe },
            new() { RoomNumber = "204", Floor = 2, RoomType = deluxe, IsActive = false }, // under maintenance
            new() { RoomNumber = "301", Floor = 3, RoomType = family },
            new() { RoomNumber = "302", Floor = 3, RoomType = family },
            new() { RoomNumber = "401", Floor = 4, RoomType = executive },
            new() { RoomNumber = "402", Floor = 4, RoomType = executive },
        };
        db.Rooms.AddRange(rooms);

        var ali = new Customer { FullName = "Ali Khan", Email = "ali@example.com", Phone = "0300-1234567" };
        var sara = new Customer { FullName = "Sara Ahmed", Email = "sara@example.com", Phone = "0311-7654321" };
        var humdan = new Customer { FullName = "Muhammad Humdan", Email = "humdan@example.com", Phone = "0345-1112223" };
        db.Customers.AddRange(ali, sara, humdan);

        // A few sample bookings built from today's date so the demo always looks "alive".
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.Reservations.AddRange(
            new Reservation { Room = rooms[0], Customer = ali, CheckInDate = today.AddDays(-1), CheckOutDate = today.AddDays(2), Guests = 1, TotalPrice = 3 * 8000, Status = ReservationStatus.CheckedIn },
            new Reservation { Room = rooms[3], Customer = sara, CheckInDate = today.AddDays(3), CheckOutDate = today.AddDays(6), Guests = 2, TotalPrice = 3 * 14000, Status = ReservationStatus.Confirmed },
            new Reservation { Room = rooms[7], Customer = humdan, CheckInDate = today.AddDays(10), CheckOutDate = today.AddDays(12), Guests = 3, TotalPrice = 2 * 22000, Status = ReservationStatus.Confirmed },
            new Reservation { Room = rooms[1], Customer = ali, CheckInDate = today.AddDays(-20), CheckOutDate = today.AddDays(-17), Guests = 1, TotalPrice = 3 * 8000, Status = ReservationStatus.CheckedOut },
            new Reservation { Room = rooms[9], Customer = sara, CheckInDate = today.AddDays(5), CheckOutDate = today.AddDays(7), Guests = 2, TotalPrice = 2 * 35000, Status = ReservationStatus.Cancelled }
        );

        await db.SaveChangesAsync();
    }
}
