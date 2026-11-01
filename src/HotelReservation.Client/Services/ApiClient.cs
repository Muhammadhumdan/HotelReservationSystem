using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HotelReservation.Shared.Dtos;
using HotelReservation.Shared.Enums;

namespace HotelReservation.Client.Services;

/// <summary>An error with a message that is safe to show to the user.</summary>
public class ApiException(string message, int statusCode = 0) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// The ONE class that knows how to call the Web API. Pages never build URLs themselves; they just
/// call methods like GetRoomTypesAsync(). If an API address changes, we fix it in one place.
/// </summary>
public class ApiClient(HttpClient http)
{
    // ===== Room types =====
    public Task<List<RoomTypeDto>> GetRoomTypesAsync(string? search = null) =>
        GetAsync<List<RoomTypeDto>>("api/roomtypes" + Query(("search", search)));
    public Task CreateRoomTypeAsync(RoomTypeRequest r) => SendAsync(HttpMethod.Post, "api/roomtypes", r);
    public Task UpdateRoomTypeAsync(int id, RoomTypeRequest r) => SendAsync(HttpMethod.Put, $"api/roomtypes/{id}", r);
    public Task DeleteRoomTypeAsync(int id) => SendAsync(HttpMethod.Delete, $"api/roomtypes/{id}");

    // ===== Rooms =====
    public Task<List<RoomDto>> GetRoomsAsync(int? roomTypeId = null, string? search = null, bool? isActive = null) =>
        GetAsync<List<RoomDto>>("api/rooms" + Query(("roomTypeId", roomTypeId), ("search", search), ("isActive", isActive)));
    public Task CreateRoomAsync(RoomRequest r) => SendAsync(HttpMethod.Post, "api/rooms", r);
    public Task UpdateRoomAsync(int id, RoomRequest r) => SendAsync(HttpMethod.Put, $"api/rooms/{id}", r);
    public Task DeleteRoomAsync(int id) => SendAsync(HttpMethod.Delete, $"api/rooms/{id}");
    public Task<List<AvailableRoomDto>> SearchAvailableRoomsAsync(DateOnly checkIn, DateOnly checkOut, int guests, int? roomTypeId = null, decimal? maxPrice = null) =>
        GetAsync<List<AvailableRoomDto>>("api/rooms/available" +
            Query(("checkIn", checkIn), ("checkOut", checkOut), ("guests", guests), ("roomTypeId", roomTypeId), ("maxPrice", maxPrice)));

    // ===== Customers =====
    public Task<List<CustomerDto>> GetCustomersAsync(string? search = null) =>
        GetAsync<List<CustomerDto>>("api/customers" + Query(("search", search)));
    public Task<CustomerDto> GetCustomerAsync(int id) => GetAsync<CustomerDto>($"api/customers/{id}");
    public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
    {
        try { return await GetAsync<CustomerDto>("api/customers/by-email" + Query(("email", email))); }
        catch (ApiException ex) when (ex.StatusCode == 404) { return null; }   // 404 = no such customer
    }
    public Task<CustomerDto> CreateCustomerAsync(CustomerRequest r) => SendAsync<CustomerDto>(HttpMethod.Post, "api/customers", r);
    public Task UpdateCustomerAsync(int id, CustomerRequest r) => SendAsync(HttpMethod.Put, $"api/customers/{id}", r);
    public Task DeleteCustomerAsync(int id) => SendAsync(HttpMethod.Delete, $"api/customers/{id}");

    // ===== Reservations =====
    public Task<List<ReservationDto>> GetReservationsAsync(int? customerId = null, ReservationStatus? status = null,
        DateOnly? from = null, DateOnly? to = null, string? search = null) =>
        GetAsync<List<ReservationDto>>("api/reservations" +
            Query(("customerId", customerId), ("status", status), ("from", from), ("to", to), ("search", search)));
    public Task<ReservationDto> CreateReservationAsync(ReservationRequest r) => SendAsync<ReservationDto>(HttpMethod.Post, "api/reservations", r);
    public Task<ReservationDto> ChangeReservationStatusAsync(int id, ReservationStatus status) =>
        SendAsync<ReservationDto>(HttpMethod.Put, $"api/reservations/{id}/status", new ChangeStatusRequest { Status = status });

    // ===== plumbing =====

    /// <summary>Builds "?a=1&b=2" and skips empty values, so filters are optional.</summary>
    private static string Query(params (string Key, object? Value)[] pairs)
    {
        var parts = new List<string>();
        foreach (var (key, value) in pairs)
        {
            string? text = value switch
            {
                null => null,
                string s => string.IsNullOrWhiteSpace(s) ? null : s.Trim(),
                DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                Enum e => Convert.ToInt32(e).ToString(CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
            if (text is not null) parts.Add($"{key}={Uri.EscapeDataString(text)}");
        }
        return parts.Count == 0 ? "" : "?" + string.Join("&", parts);
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await SendCoreAsync(HttpMethod.Get, url, null);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        var response = await SendCoreAsync(method, url, body);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task SendAsync(HttpMethod method, string url, object? body = null) =>
        await SendCoreAsync(method, url, body);

    private async Task<HttpResponseMessage> SendCoreAsync(HttpMethod method, string url, object? body)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = JsonContent.Create(body, body.GetType());

            var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync();
                throw new ApiException(ExtractMessage(text, response.StatusCode), (int)response.StatusCode);
            }
            return response;
        }
        catch (HttpRequestException)
        {
            // Happens when the API is not running or the address/port is wrong.
            throw new ApiException("Cannot reach the hotel API. Make sure the HotelReservation.Api project is running.");
        }
    }

    /// <summary>Pulls a human-friendly sentence out of the API's JSON error.</summary>
    private static string ExtractMessage(string body, HttpStatusCode code)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Validation errors look like: { "errors": { "Name": ["Name is required."] } }
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", messages);
                if (joined.Length > 0) return joined;
            }
            // Business-rule errors look like: { "title": "...", "detail": "Room 101 is already booked..." }
            if (root.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString()!;
            if (root.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString()!;
        }
        catch (JsonException) { /* body was not JSON; fall through */ }

        return $"The request failed ({(int)code} {code}).";
    }
}
