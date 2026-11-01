using HotelReservation.Api.Data;
using HotelReservation.Api.Models;
using HotelReservation.Api.Services;
using HotelReservation.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Controllers;

/// <summary>CRUD for room types. [ApiController] automatically checks the validation attributes on the request.</summary>
[ApiController]
[Route("api/[controller]")]
public class RoomTypesController(HotelDbContext db) : ControllerBase
{
    // GET api/roomtypes?search=deluxe
    [HttpGet]
    public async Task<ActionResult<List<RoomTypeDto>>> GetAll([FromQuery] string? search)
    {
        var query = db.RoomTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Name.Contains(search));

        return await query.OrderBy(t => t.PricePerNight).Select(t => new RoomTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            PricePerNight = t.PricePerNight,
            Capacity = t.Capacity,
            RoomCount = t.Rooms.Count
        }).ToListAsync();
    }

    // GET api/roomtypes/3
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomTypeDto>> GetById(int id)
    {
        var dto = await db.RoomTypes.AsNoTracking().Where(t => t.Id == id).Select(t => new RoomTypeDto
        {
            Id = t.Id, Name = t.Name, Description = t.Description,
            PricePerNight = t.PricePerNight, Capacity = t.Capacity, RoomCount = t.Rooms.Count
        }).FirstOrDefaultAsync();

        return dto is null ? NotFound() : dto;
    }

    // POST api/roomtypes
    [HttpPost]
    public async Task<ActionResult<RoomTypeDto>> Create(RoomTypeRequest request)
    {
        if (await db.RoomTypes.AnyAsync(t => t.Name == request.Name))
            throw new BusinessRuleException($"A room type named '{request.Name}' already exists.", StatusCodes.Status409Conflict);

        var entity = new RoomType
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            PricePerNight = request.PricePerNight,
            Capacity = request.Capacity
        };
        db.RoomTypes.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, (await GetById(entity.Id)).Value);
    }

    // PUT api/roomtypes/3
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RoomTypeRequest request)
    {
        var entity = await db.RoomTypes.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.RoomTypes.AnyAsync(t => t.Name == request.Name && t.Id != id))
            throw new BusinessRuleException($"A room type named '{request.Name}' already exists.", StatusCodes.Status409Conflict);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.PricePerNight = request.PricePerNight;
        entity.Capacity = request.Capacity;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE api/roomtypes/3
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.RoomTypes.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.Rooms.AnyAsync(r => r.RoomTypeId == id))
            throw new BusinessRuleException("This room type still has rooms. Move or delete those rooms first.", StatusCodes.Status409Conflict);

        db.RoomTypes.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
