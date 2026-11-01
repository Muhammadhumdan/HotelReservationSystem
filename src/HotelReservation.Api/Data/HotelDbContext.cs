using HotelReservation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Api.Data;

/// <summary>
/// The DbContext is the "bridge" between C# and SQL Server.
/// Each DbSet is one table. OnModelCreating describes keys, relationships and rules.
/// </summary>
public class HotelDbContext(DbContextOptions<HotelDbContext> options) : DbContext(options)
{
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomType>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(300);
            e.Property(x => x.PricePerNight).HasPrecision(12, 2);
            e.HasIndex(x => x.Name).IsUnique();              // no two room types with the same name
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(x => x.RoomNumber).HasMaxLength(10).IsRequired();
            e.HasIndex(x => x.RoomNumber).IsUnique();         // no two rooms with the same number

            // One RoomType -> many Rooms. Restrict = you cannot delete a type while rooms use it.
            e.HasOne(x => x.RoomType)
             .WithMany(t => t.Rooms)
             .HasForeignKey(x => x.RoomTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();              // one account per email
        });

        modelBuilder.Entity<Reservation>(e =>
        {
            e.Property(x => x.TotalPrice).HasPrecision(12, 2);

            e.HasOne(x => x.Room)
             .WithMany(r => r.Reservations)
             .HasForeignKey(x => x.RoomId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Customer)
             .WithMany(c => c.Reservations)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            // Index that makes the "is this room free on these dates?" query fast.
            e.HasIndex(x => new { x.RoomId, x.CheckInDate, x.CheckOutDate });
        });
    }
}
