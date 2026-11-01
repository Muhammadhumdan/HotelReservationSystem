using HotelReservation.Api.Data;
using HotelReservation.Api.Models;
using HotelReservation.Api.Services;
using HotelReservation.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(HotelDbContext db, IReservationService reservations) : ControllerBase
{
    // GET api/rooms?roomTypeId=2&search=20&isActive=true
    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll([FromQuery] int? roomTypeId, [FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var query = db.Rooms.AsNoTracking().AsQueryable();
        if (roomTypeId.HasValue) query = query.Where(r => r.RoomTypeId == roomTypeId.Value);
        if (isActive.HasValue) query = query.Where(r => r.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.RoomNumber.Contains(search));

        return await Project(query).OrderBy(r => r.RoomNumber).ToListAsync();
    }

    // GET api/rooms/available?checkIn=2026-11-10&checkOut=2026-11-13&guests=2&roomTypeId=&maxPrice=
    // Used by customers to SEARCH rooms that are free for their whole stay.
    [HttpGet("available")]
    public async Task<ActionResult<List<AvailableRoomDto>>> Available(
        [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut,
        [FromQuery] int guests = 1, [FromQuery] int? roomTypeId = null, [FromQuery] decimal? maxPrice = null)
    {
        return await reservations.SearchAvailableRoomsAsync(checkIn, checkOut, guests, roomTypeId, maxPrice);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var dto = await Project(db.Rooms.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(RoomRequest request)
    {
        await EnsureValidAsync(request, null);

        var entity = new Room
        {
            RoomNumber = request.RoomNumber.Trim(),
            Floor = request.Floor,
            RoomTypeId = request.RoomTypeId,
            IsActive = request.IsActive
        };
        db.Rooms.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, (await GetById(entity.Id)).Value);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RoomRequest request)
    {
        var entity = await db.Rooms.FindAsync(id);
        if (entity is null) return NotFound();

        await EnsureValidAsync(request, id);

        entity.RoomNumber = request.RoomNumber.Trim();
        entity.Floor = request.Floor;
        entity.RoomTypeId = request.RoomTypeId;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Rooms.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.Reservations.AnyAsync(r => r.RoomId == id))
            throw new BusinessRuleException("This room has reservations on record. Mark it inactive instead of deleting it.", StatusCodes.Status409Conflict);

        db.Rooms.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- helpers ----------

    /// <summary>Checks the room type exists and the room number is unique.</summary>
    private async Task EnsureValidAsync(RoomRequest request, int? existingId)
    {
        if (!await db.RoomTypes.AnyAsync(t => t.Id == request.RoomTypeId))
            throw new BusinessRuleException("The selected room type does not exist.");

        if (await db.Rooms.AnyAsync(r => r.RoomNumber == request.RoomNumber && r.Id != existingId))
            throw new BusinessRuleException($"Room number {request.RoomNumber} already exists.", StatusCodes.Status409Conflict);
    }

    /// <summary>Turns Room rows into RoomDto, including the automatically calculated "occupied today" flag.</summary>
    private static IQueryable<RoomDto> Project(IQueryable<Room> query)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return query.Select(r => new RoomDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            Floor = r.Floor,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType.Name,
            PricePerNight = r.RoomType.PricePerNight,
            Capacity = r.RoomType.Capacity,
            IsActive = r.IsActive,
            IsOccupiedToday = r.Reservations.Any(x =>
                ReservationService.BlockingStatuses.Contains(x.Status)
                && x.CheckInDate <= today && x.CheckOutDate > today)
        });
    }
}
