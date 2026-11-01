using HotelReservation.Api.Data;
using HotelReservation.Api.Services;
using HotelReservation.Shared.Dtos;
using HotelReservation.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(HotelDbContext db, IReservationService service) : ControllerBase
{
    // GET api/reservations?customerId=1&status=1&from=2026-11-01&to=2026-11-30&search=ali
    // Admin sees everything; a customer passes customerId to see ONLY their own booking history.
    [HttpGet]
    public async Task<ActionResult<List<ReservationDto>>> GetAll(
        [FromQuery] int? customerId, [FromQuery] int? roomId, [FromQuery] ReservationStatus? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? search)
    {
        var query = db.Reservations.AsNoTracking().AsQueryable();

        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);
        if (roomId.HasValue) query = query.Where(r => r.RoomId == roomId.Value);
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (from.HasValue) query = query.Where(r => r.CheckOutDate > from.Value);   // stays that end after 'from'
        if (to.HasValue) query = query.Where(r => r.CheckInDate <= to.Value);       // stays that start before 'to'
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Customer.FullName.Contains(search) || r.Room.RoomNumber.Contains(search));

        return await query.OrderByDescending(r => r.CheckInDate).Select(ReservationService.ToDto).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetById(int id)
    {
        var dto = await db.Reservations.AsNoTracking().Where(r => r.Id == id)
            .Select(ReservationService.ToDto).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    // POST api/reservations  -> makes a booking (all rules are checked inside the service)
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(ReservationRequest request)
    {
        var created = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT api/reservations/5/status   body: { "status": 4 }  -> cancel / check-in / check-out
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ReservationDto>> ChangeStatus(int id, ChangeStatusRequest request)
    {
        return await service.ChangeStatusAsync(id, request.Status);
    }
}
