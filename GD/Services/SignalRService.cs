using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using System.Threading.Tasks;

namespace GD.Services
{
    public class SignalRService : IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly ISnackbar _snackbar;
        private HubConnection _hubConnection;
        private bool _isConnected;

        public SignalRService(NavigationManager navigationManager, IJSRuntime jsRuntime, ISnackbar snackbar)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
            _snackbar = snackbar;
        }

        public async Task InitializeAsync()
        {
            if (_hubConnection == null)
            {
                // Create the connection
                var url = new Uri(_navigationManager.BaseUri + "poshub");
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();

                // Set up handlers
                _hubConnection.On<string>("ReceiveNotification", async (message) =>
                {
                    // Show snackbar notification
                    _snackbar.Add(message, Severity.Info, config =>
                    {
                        config.VisibleStateDuration = 10000; // 10 seconds
                        config.ShowCloseIcon = true;
                    });

                    // Show browser notification if supported
                    await ShowBrowserNotificationAsync(message);
                });

                // Start the connection
                await _hubConnection.StartAsync();
                _isConnected = true;
            }
        }

        private async Task ShowBrowserNotificationAsync(string message)
        {
            try
            {
                // Check if notifications are supported and permissions are granted
                await _jsRuntime.InvokeVoidAsync("showNotification", "GoodDeed", message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing notification: {ex.Message}");
            }
        }

        public bool IsConnected => _isConnected;

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
                _isConnected = false;
            }
        }
    }
} 