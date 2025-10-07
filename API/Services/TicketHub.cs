using System.Security.Claims;
using System.Collections.Concurrent;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

[Authorize] 
public class TicketHub : Hub
{
    private readonly AppDBContext _db;
    public TicketHub(AppDBContext db) => _db = db;

    private static string G(int id) => $"ticket:{id}";

    private class ConnInfo
    {
        public bool IsStaff { get; set; }
        public HashSet<int> Tickets { get; } = new();
    }

    private static readonly ConcurrentDictionary<string, ConnInfo> _conns = new();
    private static readonly ConcurrentDictionary<int, HashSet<string>> _staffByTicket = new(); 

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_conns.TryRemove(Context.ConnectionId, out var info))
        {
            foreach (var tid in info.Tickets)
            {
                if (info.IsStaff)
                {
                    RemoveStaffConn(tid, Context.ConnectionId, out var countAfter);
                    await Clients.Group(G(tid)).SendAsync("AgentPresenceChanged", new
                    {
                        TicketId = tid,
                        StaffOnline = countAfter > 0,
                        Count = countAfter
                    });
                }

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, G(tid));
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinTicket(int ticketId)
    {
        var user = Context.User!;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var t = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticketId);
        if (t is null) { await Clients.Caller.SendAsync("Error", "Ticket not found"); return; }

        var isOwner = t.CustomerUserId == userId;
        var isStaff = user.IsInRole("Admin") || user.IsInRole("Manager");
        if (!isOwner && !isStaff) { await Clients.Caller.SendAsync("Error", "Access denied"); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, G(ticketId));

        var info = _conns.GetOrAdd(Context.ConnectionId, _ => new ConnInfo());
        info.IsStaff = isStaff; 
        info.Tickets.Add(ticketId);

        int staffCount;
        if (isStaff)
        {
            AddStaffConn(ticketId, Context.ConnectionId, out staffCount);
        }
        else
        {
            staffCount = StaffCount(ticketId);
        }

        await Clients.Caller.SendAsync("Joined", ticketId);

        await Clients.Caller.SendAsync("AgentPresenceChanged", new
        {
            TicketId = ticketId,
            StaffOnline = staffCount > 0,
            Count = staffCount
        });

        if (isStaff)
        {
            await Clients.Group(G(ticketId)).SendAsync("AgentPresenceChanged", new
            {
                TicketId = ticketId,
                StaffOnline = staffCount > 0,
                Count = staffCount
            });
        }
    }

    private static void AddStaffConn(int ticketId, string connId, out int countAfter)
    {
        var set = _staffByTicket.GetOrAdd(ticketId, _ => new HashSet<string>());
        lock (set) { set.Add(connId); countAfter = set.Count; }
    }

    private static void RemoveStaffConn(int ticketId, string connId, out int countAfter)
    {
        countAfter = 0;
        if (_staffByTicket.TryGetValue(ticketId, out var set))
        {
            lock (set)
            {
                set.Remove(connId);
                countAfter = set.Count;
                if (set.Count == 0)
                    _staffByTicket.TryRemove(ticketId, out _);
            }
        }
    }

    private static int StaffCount(int ticketId)
    {
        if (_staffByTicket.TryGetValue(ticketId, out var set))
        {
            lock (set) return set.Count;
        }
        return 0;
    }
}
