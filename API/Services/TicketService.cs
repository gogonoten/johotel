using System.Security.Claims;
using API.Data;
using API.Services.Mail;
using DomainModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

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
    private readonly IMailService _mail;                    
    private readonly IHttpContextAccessor _http;           
    private readonly MailSettings _mailCfg;                 

    public TicketService(
        AppDBContext db,
        IHubContext<TicketHub> hub,                        
        IMailService mail,
        IHttpContextAccessor http,
        IOptions<MailSettings> mailCfg)
    {
        _db = db;
        _hub = hub;                                        
        _mail = mail;
        _http = http;
        _mailCfg = mailCfg.Value;
    }



    private static int GetUserId(ClaimsPrincipal u) => int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string GetUserName(ClaimsPrincipal u) => u.Identity?.Name ?? "User";
    private static bool IsStaff(ClaimsPrincipal u) => u.IsInRole("Admin") || u.IsInRole("Manager");


    //Alle der skal have notifikation på email
    private async Task<List<(int Id, string Email, string Name)>> GetNotifyRecipientsAsync(int excludeUserId = 0)
    {
        var list = await _db.Users
            .Where(u => (u.RoleId == 1 || u.RoleId == 2)
                        && u.Id != excludeUserId
                        && u.Email != null && u.Email != "")
            .Select(u => new
            {
                u.Id,
                u.Email,
                Name = u.Username ?? u.Email
            })
            .ToListAsync();

        return list.Select(x => (x.Id, x.Email, x.Name)).ToList();
    }






    private string BuildStaffLink(int ticketId)
    {
        var req = _http.HttpContext?.Request;
        var scheme = string.IsNullOrWhiteSpace(req?.Scheme) ? "https" : req!.Scheme;
        var host = req?.Host.HasValue == true ? req.Host.Value : "localhost";
        return $"{scheme}://{host}/ticketadmin/{ticketId}";
    }

    private string BuildUserLink(int ticketId)
    {
        var req = _http.HttpContext?.Request;
        var scheme = string.IsNullOrWhiteSpace(req?.Scheme) ? "https" : req!.Scheme;
        var host = req?.Host.HasValue == true ? req.Host.Value : "localhost";
        return $"{scheme}://{host}/tickets/{ticketId}";
    }







    public async Task<Ticket> CreateAsync(CreateTicketDto dto, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var userName = GetUserName(user);
        var count = await _db.Tickets.CountAsync();
        var number = $"Chat-ID: {(count + 1).ToString().PadLeft(6, '0')}";

        var t = new Ticket
        {
            Number = number,
            Title = dto.Title,
            BookingId = dto.BookingId,
            CustomerUserId = userId,
            Department = dto.Department,
            Priority = dto.Priority,
            Status = TicketStatus.Ny,
            FirstResponseDueAt = DateTimeOffset.UtcNow.AddHours(8),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _db.Tickets.Add(t);

        _db.TicketMessages.Add(new TicketMessage
        {
            Ticket = t,
            AuthorUserId = userId,
            AuthorName = userName,
            IsInternal = false,
            Content = dto.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync();

        var cust = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.Username })
            .FirstAsync();

        var staffLink = BuildStaffLink(t.Id);
        var userLink = BuildUserLink(t.Id);
        var brand = string.IsNullOrWhiteSpace(_mailCfg.FromName) ? "JoHotel" : _mailCfg.FromName;

        var recipients = await GetNotifyRecipientsAsync(excludeUserId: userId);
        if (recipients.Count > 0)
        {
            _ = _mail.SendTicketCreatedStaffAsync(
                recipients.Select(r => r.Email),
                t.Number, t.Title, cust.Username ?? userName, t.Department.ToString(), staffLink, brand);
        }

        if (!string.IsNullOrWhiteSpace(cust.Email))
        {
            _ = _mail.SendTicketCreatedUserAsync(
                cust.Email, cust.Username ?? userName, t.Number, t.Title, userLink, brand);
        }

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
        var t = await _db.Tickets
            .Include(x => x.CustomerUser)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException("Ticket");

        var authorUserId = GetUserId(user);
        var isOwner = t.CustomerUserId == authorUserId;
        var isStaff = IsStaff(user);
        if (!isOwner && !isStaff) throw new UnauthorizedAccessException();

        var msg = new TicketMessage
        {
            TicketId = id,
            AuthorUserId = authorUserId,
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

        if (dto.IsInternal) return;

        var staffLink = BuildStaffLink(t.Id);
        var userLink = BuildUserLink(t.Id);
        var brand = string.IsNullOrWhiteSpace(_mailCfg.FromName) ? "JoHotel" : _mailCfg.FromName;
        var preview = (dto.Content ?? string.Empty);
        if (preview.Length > 300) preview = preview[..300] + "…";

        if (isStaff)
        {
            if (!string.IsNullOrWhiteSpace(t.CustomerUser.Email))
            {
                _ = _mail.SendTicketReplyToUserAsync(
                    t.CustomerUser.Email,
                    t.CustomerUser.Username ?? t.CustomerUser.Email,
                    t.Number, preview, userLink, brand);
            }
        }
        else
        {
            var recipients = await GetNotifyRecipientsAsync(excludeUserId: authorUserId);
            if (recipients.Count > 0)
            {
                _ = _mail.SendTicketReplyToStaffAsync(
                    recipients.Select(r => r.Email),
                    t.Number,
                    t.CustomerUser.Username ?? t.CustomerUser.Email,
                    preview, staffLink, brand);
            }
        }
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
