using HotelReservation.Api.Data;
using HotelReservation.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1) Database: tell EF Core to use SQL Server with the connection string from appsettings.json
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HotelDb")));

// 2) Our business-logic service (a new instance per web request)
builder.Services.AddScoped<IReservationService, ReservationService>();

// 3) Friendly JSON errors for business-rule problems
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddProblemDetails();

// 4) CORS: browsers block a website from calling a DIFFERENT address unless the API allows it.
//    The Blazor client runs on another port, so we allow it here.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy("BlazorClient", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Create the database (if missing) and add sample data on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("BlazorClient");
app.MapControllers();

app.Run();
