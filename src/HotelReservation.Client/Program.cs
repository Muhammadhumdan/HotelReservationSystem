using HotelReservation.Client;
using HotelReservation.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Address of the Web API (change it in wwwroot/appsettings.json if your API uses another port).
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7100/";

// HttpClient = the "phone" our app uses to call the API.
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddScoped<ApiClient>();       // one place that knows every API URL
builder.Services.AddScoped<SessionState>();    // remembers who is "signed in"

await builder.Build().RunAsync();
