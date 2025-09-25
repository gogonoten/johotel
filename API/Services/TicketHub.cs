using System.Security.Claims;
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

    public async Task JoinTicket(int ticketId)
    {
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = Context.User!.FindFirstValue(ClaimTypes.Role) ?? "Customer";

        var t = await _db.Tickets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ticketId);
        if (t is null) { await Clients.Caller.SendAsync("Error", "Ticket not found"); return; }

        var isOwner = t.CustomerUserId == userId;
        var isStaff = role is "Admin" or "Manager";
        if (!isOwner && !isStaff) { await Clients.Caller.SendAsync("Error", "Access denied"); return; }

        await Groups.AddToGroupAsync(Context.ConnectionId, G(ticketId));
        await Clients.Caller.SendAsync("Joined", ticketId);
    }
}
