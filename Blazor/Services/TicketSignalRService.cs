using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Blazor.Services;

public class TicketSignalRService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _conn;

    public bool IsConnected => _conn?.State == HubConnectionState.Connected;

    public TicketSignalRService(NavigationManager nav) { _nav = nav; }

    public async Task StartAsync(string? jwt = null, string? hubUrl = null)
    {
        var url = hubUrl ?? _nav.ToAbsoluteUri("/api/hubs/tickets").ToString();

        if (_conn is null)
        {
            _conn = new HubConnectionBuilder()
                .WithUrl(url, o =>
                {
                    if (!string.IsNullOrWhiteSpace(jwt))
                        o.AccessTokenProvider = () => Task.FromResult(jwt)!;
                })
                .WithAutomaticReconnect()
                .Build();
        }

        if (_conn.State == HubConnectionState.Disconnected)
            await _conn.StartAsync();
    }

    public Task JoinAsync(int ticketId) => _conn!.InvokeAsync("JoinTicket", ticketId);

    public IDisposable OnMessageReceived<T>(Action<T> handler) => _conn!.On("MessageReceived", handler);

    public IDisposable OnJoined<T>(Action<T> handler) => _conn!.On("Joined", handler);
    public IDisposable OnError<T>(Action<T> handler) => _conn!.On("Error", handler);

    public async ValueTask DisposeAsync()
    {
        if (_conn != null) await _conn.DisposeAsync();
    }
}
