using System.Data;
using HotelReservation.Api.Data;
using HotelReservation.Api.Models;
using HotelReservation.Shared.Dtos;
using HotelReservation.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Services;

public interface IReservationService
{
    Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(DateOnly checkIn, DateOnly checkOut, int guests, int? roomTypeId, decimal? maxPrice);
    Task<ReservationDto> CreateAsync(ReservationRequest request);
    Task<ReservationDto> ChangeStatusAsync(int id, ReservationStatus newStatus);
}

/// <summary>
/// ALL the booking business logic lives here (not in the controller). Think of the controller as the
/// receptionist who takes your request, and this service as the manager who knows the hotel rules.
/// </summary>
public class ReservationService(HotelDbContext db) : IReservationService
{
    /// <summary>Only these statuses occupy a room. Cancelled / CheckedOut bookings free the room again.</summary>
    public static readonly ReservationStatus[] BlockingStatuses = { ReservationStatus.Confirmed, ReservationStatus.CheckedIn };

    // ---------- SEARCH ----------
    public async Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(
        DateOnly checkIn, DateOnly checkOut, int guests, int? roomTypeId, decimal? maxPrice)
    {
        ValidateDates(checkIn, checkOut);
        int nights = checkOut.DayNumber - checkIn.DayNumber;

        // A room is available when NO blocking reservation overlaps the wanted dates.
        // Two date ranges overlap when: existing.CheckIn < wantedCheckOut  AND  existing.CheckOut > wantedCheckIn
        var query = db.Rooms.AsNoTracking()
            .Include(r => r.RoomType)
            .Where(r => r.IsActive
                     && r.RoomType.Capacity >= guests
                     && !r.Reservations.Any(x => BlockingStatuses.Contains(x.Status)
                                              && x.CheckInDate < checkOut
                                              && x.CheckOutDate > checkIn));

        if (roomTypeId.HasValue) query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
        if (maxPrice.HasValue) query = query.Where(r => r.RoomType.PricePerNight <= maxPrice.Value);

        var rooms = await query.OrderBy(r => r.RoomType.PricePerNight).ThenBy(r => r.RoomNumber).ToListAsync();

        return rooms.Select(r => new AvailableRoomDto
        {
            RoomId = r.Id,
            RoomNumber = r.RoomNumber,
            Floor = r.Floor,
            RoomTypeName = r.RoomType.Name,
            Description = r.RoomType.Description,
            Capacity = r.RoomType.Capacity,
            PricePerNight = r.RoomType.PricePerNight,
            Nights = nights,
            TotalPrice = r.RoomType.PricePerNight * nights
        }).ToList();
    }

    // ---------- CREATE BOOKING ----------
    public async Task<ReservationDto> CreateAsync(ReservationRequest request)
    {
        ValidateDates(request.CheckInDate, request.CheckOutDate);

        if (request.CheckInDate < DateOnly.FromDateTime(DateTime.Today))
            throw new BusinessRuleException("Check-in date cannot be in the past.");

        var customer = await db.Customers.FindAsync(request.CustomerId)
            ?? throw new BusinessRuleException("Customer not found.", StatusCodes.Status404NotFound);

        var room = await db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == request.RoomId)
            ?? throw new BusinessRuleException("Room not found.", StatusCodes.Status404NotFound);

        if (!room.IsActive)
            throw new BusinessRuleException($"Room {room.RoomNumber} is not available (under maintenance).", StatusCodes.Status409Conflict);

        if (request.Guests > room.RoomType.Capacity)
            throw new BusinessRuleException($"Room {room.RoomNumber} fits at most {room.RoomType.Capacity} guest(s).");

        // A SERIALIZABLE transaction makes "check then save" safe: if two people click "Book" for the
        // same room at the same moment, the database makes them wait in line, so only ONE can win.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        bool clash = await db.Reservations.AnyAsync(x =>
            x.RoomId == room.Id
            && BlockingStatuses.Contains(x.Status)
            && x.CheckInDate < request.CheckOutDate
            && x.CheckOutDate > request.CheckInDate);

        if (clash)
            throw new BusinessRuleException(
                $"Room {room.RoomNumber} is already booked for some of these dates. Please choose other dates or another room.",
                StatusCodes.Status409Conflict);

        int nights = request.CheckOutDate.DayNumber - request.CheckInDate.DayNumber;

        var reservation = new Reservation
        {
            RoomId = room.Id,
            CustomerId = customer.Id,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Guests = request.Guests,
            TotalPrice = room.RoomType.PricePerNight * nights,   // price is calculated by the SERVER, never trusted from the client
            Status = ReservationStatus.Confirmed
        };

        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return (await GetDtoAsync(reservation.Id))!;
    }

    // ---------- CHANGE STATUS (check-in, check-out, cancel) ----------
    public async Task<ReservationDto> ChangeStatusAsync(int id, ReservationStatus newStatus)
    {
        var reservation = await db.Reservations.FindAsync(id)
            ?? throw new BusinessRuleException("Reservation not found.", StatusCodes.Status404NotFound);

        // Allowed moves, like a one-way staircase.
        bool allowed = (reservation.Status, newStatus) switch
        {
            (ReservationStatus.Confirmed, ReservationStatus.CheckedIn) => true,
            (ReservationStatus.Confirmed, ReservationStatus.Cancelled) => true,
            (ReservationStatus.CheckedIn, ReservationStatus.CheckedOut) => true,
            _ => false
        };

        if (!allowed)
            throw new BusinessRuleException($"A {reservation.Status} reservation cannot be changed to {newStatus}.", StatusCodes.Status409Conflict);

        reservation.Status = newStatus;
        await db.SaveChangesAsync();
        return (await GetDtoAsync(id))!;
    }

    // ---------- helpers ----------
    private static void ValidateDates(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn)
            throw new BusinessRuleException("Check-out date must be after the check-in date.");
        if (checkOut.DayNumber - checkIn.DayNumber > 30)
            throw new BusinessRuleException("A single booking can be at most 30 nights.");
    }

    public Task<ReservationDto?> GetDtoAsync(int id) =>
        db.Reservations.AsNoTracking().Where(r => r.Id == id).Select(ToDto).FirstOrDefaultAsync();

    /// <summary>Converts a Reservation (database shape) into a ReservationDto (API shape).</summary>
    public static readonly System.Linq.Expressions.Expression<Func<Reservation, ReservationDto>> ToDto = r => new ReservationDto
    {
        Id = r.Id,
        RoomId = r.RoomId,
        RoomNumber = r.Room.RoomNumber,
        RoomTypeName = r.Room.RoomType.Name,
        CustomerId = r.CustomerId,
        CustomerName = r.Customer.FullName,
        CheckInDate = r.CheckInDate,
        CheckOutDate = r.CheckOutDate,
        Guests = r.Guests,
        TotalPrice = r.TotalPrice,
        Status = r.Status,
        CreatedAt = r.CreatedAt
    };
}
