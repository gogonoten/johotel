using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Generic;

namespace Blazor.Services;

public class TicketSignalRService : IAsyncDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _conn;

    private readonly HashSet<int> _joinedTickets = new();

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

            _conn.Reconnected += async _ =>
            {
                foreach (var tid in _joinedTickets.ToArray())
                {
                    try { await _conn.InvokeAsync("JoinTicket", tid); }
                    catch {  }
                }
            };
        }

        if (_conn.State == HubConnectionState.Disconnected)
            await _conn.StartAsync();
    }

  
    public async Task EnsureJoinAsync(int ticketId)
    {
        _joinedTickets.Add(ticketId);
        await _conn!.InvokeAsync("JoinTicket", ticketId);
    }

    public IDisposable OnMessageReceived<T>(Action<T> handler) => _conn!.On("MessageReceived", handler);

    public IDisposable OnJoined<T>(Action<T> handler) => _conn!.On("Joined", handler);
    public IDisposable OnError<T>(Action<T> handler) => _conn!.On("Error", handler);
    public IDisposable OnAgentPresenceChanged<T>(Action<T> handler) => _conn!.On("AgentPresenceChanged", handler);


    public async ValueTask DisposeAsync()
    {
        if (_conn != null) await _conn.DisposeAsync();
    }
}
