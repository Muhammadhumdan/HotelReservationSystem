using HotelReservation.Shared.Dtos;
using Microsoft.JSInterop;

namespace HotelReservation.Client.Services;

/// <summary>
/// Remembers WHO is using the site: nobody, an administrator, or a customer.
/// It also saves this in the browser's localStorage so a page refresh does not sign you out.
/// NOTE: this is a simple "demo mode" sign-in (no passwords). See README for how to add real login.
/// </summary>
public class SessionState(IJSRuntime js, ApiClient api)
{
    private Task? _initTask;

    public bool IsAdmin { get; private set; }
    public CustomerDto? Customer { get; private set; }
    public bool IsCustomer => Customer is not null;
    public bool IsSignedIn => IsAdmin || IsCustomer;

    /// <summary>Raised whenever the sign-in state changes so the menu can redraw itself.</summary>
    public event Action? Changed;

    /// <summary>Safe to call from many components at once; the real work runs only one time.</summary>
    public Task EnsureInitializedAsync() => _initTask ??= LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            var role = await js.InvokeAsync<string?>("localStorage.getItem", "hotel.role");
            if (role == "admin")
            {
                IsAdmin = true;
            }
            else if (role == "customer")
            {
                var idText = await js.InvokeAsync<string?>("localStorage.getItem", "hotel.customerId");
                if (int.TryParse(idText, out var id)) Customer = await api.GetCustomerAsync(id);
            }
        }
        catch
        {
            // If anything fails (API offline, storage blocked) the user simply starts signed out.
        }
        Changed?.Invoke();
    }

    public async Task SignInAsAdminAsync()
    {
        IsAdmin = true;
        Customer = null;
        await js.InvokeVoidAsync("localStorage.setItem", "hotel.role", "admin");
        Changed?.Invoke();
    }

    public async Task SignInAsCustomerAsync(CustomerDto customer)
    {
        IsAdmin = false;
        Customer = customer;
        await js.InvokeVoidAsync("localStorage.setItem", "hotel.role", "customer");
        await js.InvokeVoidAsync("localStorage.setItem", "hotel.customerId", customer.Id.ToString());
        Changed?.Invoke();
    }

    public async Task SignOutAsync()
    {
        IsAdmin = false;
        Customer = null;
        await js.InvokeVoidAsync("localStorage.removeItem", "hotel.role");
        await js.InvokeVoidAsync("localStorage.removeItem", "hotel.customerId");
        Changed?.Invoke();
    }
}
