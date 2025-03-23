using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GD;
using MudBlazor.Services;
using Blazored.LocalStorage;
using Darnton.Blazor.DeviceInterop.Geolocation;
using GD.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5123/") });
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services.AddScoped<HttpService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GeolocationService>();
builder.Services.AddScoped<SignalRService>();

await builder.Build().RunAsync();

