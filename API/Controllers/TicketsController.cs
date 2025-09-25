using API.Services;
using DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/tickets")]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _svc;
        public TicketsController(ITicketService svc) { _svc = svc; }

        [HttpPost]
        public async Task<ActionResult<TicketDto>> Create(CreateTicketDto dto)
        {
            var t = await _svc.CreateAsync(dto, User);
            var outDto = new TicketDto
            {
                Id = t.Id,
                Number = t.Number,
                Title = t.Title,
                BookingId = t.BookingId,
                Department = t.Department,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            };
            return CreatedAtAction(nameof(GetDetail), new { id = t.Id }, outDto);
        }

        [HttpGet("mine")]
        public async Task<ActionResult<List<TicketDto>>> Mine() => await _svc.ListMineAsync(User);

        [HttpGet("desk"), Authorize(Policy = "StaffOnly")]
        public async Task<ActionResult<List<TicketDto>>> Desk() => await _svc.ListForStaffAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TicketDetailDto>> GetDetail(int id)
        {
            var dto = await _svc.GetDetailAsync(id, User);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("{id:int}/messages")]
        public async Task<ActionResult> PostMessage(int id, PostTicketMessageDto dto)
        {
            await _svc.AddMessageAsync(id, dto, User);
            return NoContent();
        }
    }
}
