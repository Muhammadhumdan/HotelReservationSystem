# Margalla Stay - Hotel Reservation Management System (Task 4)

ASP.NET Core Web API + Entity Framework Core + SQL Server + Blazor WebAssembly (.NET 10).

## Projects
- `HotelReservation.Shared`  DTOs + validation rules + ReservationStatus enum (used by API and client)
- `HotelReservation.Api`     Web API: models, DbContext, seed data, booking service, controllers, Swagger
- `HotelReservation.Client`  Blazor WebAssembly website (admin + guest screens)

## How to run (Visual Studio 2026)
1. Install the .NET 10 SDK and SQL Server (Express is fine). Open SSMS once and note the server name shown on the connect screen.
2. Open `HotelReservationSystem.slnx` in Visual Studio.
3. Edit `src/HotelReservation.Api/appsettings.json` -> `ConnectionStrings:HotelDb`. Replace `.\SQLEXPRESS` with your own server name.
   Examples: `(localdb)\MSSQLLocalDB`, `localhost`, `.\SQLEXPRESS`.
4. Trust the HTTPS dev certificate (one time), in a terminal: `dotnet dev-certs https --trust`
5. Right-click the solution -> Configure Startup Projects -> Multiple startup projects -> set
   HotelReservation.Api = Start and HotelReservation.Client = Start. Press F5.
6. The database `HotelReservationDb` is created automatically on the first API start, with sample data.
   Refresh Databases in SSMS to see it.

## How to run (terminal)
    cd src/HotelReservation.Api    && dotnet run --launch-profile https
    cd src/HotelReservation.Client && dotnet run --launch-profile https   (second terminal)
API: https://localhost:7100/swagger   Client: https://localhost:7200

## Try it
- Guest: Sign in with `ali@example.com` (or create an account) -> Find a room -> Book -> My bookings.
- Admin: Home -> Continue as administrator -> manage room types, rooms, customers, reservations.
- Duplicate booking test: book room 201 for the same dates twice -> the second attempt is refused.

## Where each requirement lives
- CRUD: `Controllers/*` (room types, rooms, customers); reservations are create/status-change (cancel instead of delete)
- Relationships: `Data/HotelDbContext.cs` (RoomType 1-* Room 1-* Reservation *-1 Customer)
- Availability + no double booking: `Services/ReservationService.cs` (overlap rule + serializable transaction)
- Validation: attributes in `Shared/Dtos/*`, cross-field rules in `ReservationRequest.Validate`
- Search/filter: `GET api/rooms/available`, filters on rooms, customers and reservations

## Notes
- Sign-in is a demo (no passwords) and the API is not protected. For real use add ASP.NET Core Identity + JWT.
- The database is created with `EnsureCreated`. To use migrations instead:
  `dotnet ef migrations add Initial` then `dotnet ef database update` (run in the Api folder), and replace
  `EnsureCreatedAsync()` in `Program.cs` with `MigrateAsync()`. Delete the existing database first.
- To reset sample data: drop the `HotelReservationDb` database in SSMS and start the API again.
- If the client cannot reach the API, check the port in `Client/wwwroot/appsettings.json` and the CORS list in `Api/appsettings.json`.
