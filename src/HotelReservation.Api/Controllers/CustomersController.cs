using HotelReservation.Api.Data;
using HotelReservation.Api.Models;
using HotelReservation.Api.Services;
using HotelReservation.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController(HotelDbContext db) : ControllerBase
{
    // GET api/customers?search=ali   (search by name, email or phone)
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search)
    {
        var query = db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.Phone.Contains(search));

        return await query.OrderBy(c => c.FullName).Select(c => ToDto(c)).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : ToDto(c);
    }

    // GET api/customers/by-email?email=ali@example.com  (the simple "login" used by the Blazor client)
    [HttpGet("by-email")]
    public async Task<ActionResult<CustomerDto>> GetByEmail([FromQuery] string email)
    {
        var c = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email.Trim());
        return c is null ? NotFound() : ToDto(c);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerRequest request)
    {
        if (await db.Customers.AnyAsync(c => c.Email == request.Email))
            throw new BusinessRuleException("A customer with this email already exists.", StatusCodes.Status409Conflict);

        var entity = new Customer
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim()
        };
        db.Customers.Add(entity);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerRequest request)
    {
        var entity = await db.Customers.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.Customers.AnyAsync(c => c.Email == request.Email && c.Id != id))
            throw new BusinessRuleException("Another customer already uses this email.", StatusCodes.Status409Conflict);

        entity.FullName = request.FullName.Trim();
        entity.Email = request.Email.Trim();
        entity.Phone = request.Phone.Trim();
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.Customers.FindAsync(id);
        if (entity is null) return NotFound();

        if (await db.Reservations.AnyAsync(r => r.CustomerId == id))
            throw new BusinessRuleException("This customer has reservations on record and cannot be deleted.", StatusCodes.Status409Conflict);

        db.Customers.Remove(entity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerDto ToDto(Customer c) => new()
    {
        Id = c.Id, FullName = c.FullName, Email = c.Email, Phone = c.Phone, CreatedAt = c.CreatedAt
    };
}
