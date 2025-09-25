using System.Security.Claims;
using API.Data;
using DomainModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public interface ITicketService
{
    Task<Ticket> CreateAsync(CreateTicketDto dto, ClaimsPrincipal user);
    Task<TicketDetailDto?> GetDetailAsync(int id, ClaimsPrincipal user);
    Task AddMessageAsync(int id, PostTicketMessageDto dto, ClaimsPrincipal user);
    Task<List<TicketDto>> ListMineAsync(ClaimsPrincipal user);
    Task<List<TicketDto>> ListForStaffAsync(); 
}

public class TicketService : ITicketService
{
    private readonly AppDBContext _db;
    private readonly IHubContext<TicketHub> _hub;
    public TicketService(AppDBContext db, IHubContext<TicketHub> hub) { _db = db; _hub = hub; }

    private static int GetUserId(ClaimsPrincipal u) =>
        int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string GetUserName(ClaimsPrincipal u) => u.Identity?.Name ?? "User";
    private static bool IsStaff(ClaimsPrincipal u) => u.IsInRole("Admin") || u.IsInRole("Manager");

    public async Task<Ticket> CreateAsync(CreateTicketDto dto, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var number = $"H2-{DateTime.UtcNow:yyyy}-{(await _db.Tickets.CountAsync() + 1).ToString().PadLeft(6, '0')}";

        var t = new Ticket
        {
            Number = number,
            Title = dto.Title,
            BookingId = dto.BookingId,
            CustomerUserId = userId,
            Department = dto.Department,
            Priority = dto.Priority,
            Status = TicketStatus.New,
            FirstResponseDueAt = DateTimeOffset.UtcNow.AddHours(8),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Tickets.Add(t);

        _db.TicketMessages.Add(new TicketMessage
        {
            Ticket = t,
            AuthorUserId = userId,
            AuthorName = GetUserName(user),
            IsInternal = false,
            Content = dto.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();
        return t;
    }

    public async Task<TicketDetailDto?> GetDetailAsync(int id, ClaimsPrincipal user)
    {
        var t = await _db.Tickets
            .Include(x => x.CustomerUser)
            .Include(x => x.Booking)!.ThenInclude(b => b.Room)
            .Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t is null) return null;

        var isOwner = t.CustomerUserId == GetUserId(user);
        if (!isOwner && !IsStaff(user)) return null;

        var dto = new TicketDetailDto
        {
            Id = t.Id,
            Number = t.Number,
            Title = t.Title,
            BookingId = t.BookingId,
            Department = t.Department,
            Priority = t.Priority,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            Customer = new SimpleUserDto
            {
                Id = t.CustomerUser.Id,
                Email = t.CustomerUser.Email,
                Username = t.CustomerUser.Username,
                PhoneNumber = t.CustomerUser.PhoneNumber
            },
            Bookings = await _db.Bookings
                .Where(b => b.UserId == t.CustomerUserId)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CheckIn)
                .Select(b => new RoomBookingDto
                {
                    Id = b.Id,
                    RoomNumber = b.Room.RoomNumber,
                    RoomType = b.Room.Type,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut
                }).ToListAsync(),
            Messages = t.Messages
                .Where(m => IsStaff(user) || !m.IsInternal)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageDto
                {
                    Id = m.Id,
                    AuthorName = m.AuthorName,
                    IsInternal = m.IsInternal,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList()
        };

        return dto;
    }

    public async Task AddMessageAsync(int id, PostTicketMessageDto dto, ClaimsPrincipal user)
    {
        var t = await _db.Tickets.FindAsync(id) ?? throw new KeyNotFoundException("Ticket");

        var isOwner = t.CustomerUserId == GetUserId(user);
        var isStaff = IsStaff(user);
        if (!isOwner && !isStaff) throw new UnauthorizedAccessException();

        var msg = new TicketMessage
        {
            TicketId = id,
            AuthorUserId = GetUserId(user),
            AuthorName = GetUserName(user),
            IsInternal = dto.IsInternal,
            Content = dto.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.TicketMessages.Add(msg);

        if (t.FirstResponseAt is null && !dto.IsInternal && isStaff)
            t.FirstResponseAt = DateTimeOffset.UtcNow;

        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"ticket:{id}").SendAsync("MessageReceived", new
        {
            TicketId = id,
            AuthorUserId = msg.AuthorUserId,
            AuthorName = msg.AuthorName,
            IsInternal = msg.IsInternal,
            Content = msg.Content,
            CreatedAtUtc = msg.CreatedAt.UtcDateTime
        });
    }

    public async Task<List<TicketDto>> ListMineAsync(ClaimsPrincipal user)
    {
        var uid = GetUserId(user);
        return await _db.Tickets.Where(t => t.CustomerUserId == uid)
            .OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                Number = t.Number,
                Title = t.Title,
                BookingId = t.BookingId,
                Department = t.Department,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            }).ToListAsync();
    }

    public async Task<List<TicketDto>> ListForStaffAsync()
    {
        return await _db.Tickets
            .Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Canceled)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                Number = t.Number,
                Title = t.Title,
                BookingId = t.BookingId,
                Department = t.Department,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            }).ToListAsync();
    }
}
