using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zoomies.Data;
using Zoomies.Hubs;
using Zoomies.Models;

namespace Zoomies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactMessagesController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ContactMessagesController(ZoomiesDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<ActionResult<ContactMessageResponseDto>> CreateMessage(ContactMessageCreateDto request)
        {
            var car = await _context.Cars.FindAsync(request.CarId);
            if (car == null) return NotFound("Car listing not found.");

            var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderUserId)) return Unauthorized();

            var recipientUserId = !string.IsNullOrWhiteSpace(request.RecipientUserId)
                ? request.RecipientUserId
                : car.UserId;

            if (recipientUserId == senderUserId)
                return BadRequest("You cannot send a message to yourself.");

            var message = new ContactMessage
            {
                CarId = car.Id,
                SenderUserId = senderUserId,
                SenderName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown user",
                SenderEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                SenderPhone = request.SenderPhone,
                SellerUserId = recipientUserId,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            var response = ToResponse(message, car);
            await _hubContext.Clients.Group(recipientUserId).SendAsync("ReceiveMessage", response);

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ContactMessageResponseDto>> GetMessage(int id)
        {
            var message = await _context.ContactMessages
                .Include(m => m.Car)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null) return NotFound();
            if (!CanAccess(message)) return Forbid();

            return ToResponse(message, message.Car);
        }

        [HttpGet("inbox")]
        public async Task<ActionResult<IEnumerable<ContactMessageResponseDto>>> GetInbox()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var messages = await _context.ContactMessages
                .Include(m => m.Car)
                .Where(m => isAdmin || m.SellerUserId == currentUserId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => ToResponse(m, m.Car)).ToList();
        }

        [HttpGet("sent")]
        public async Task<ActionResult<IEnumerable<ContactMessageResponseDto>>> GetSent()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.ContactMessages
                .Include(m => m.Car)
                .Where(m => m.SenderUserId == currentUserId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => ToResponse(m, m.Car)).ToList();
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (message.SellerUserId != currentUserId && !User.IsInRole("Admin"))
                return Forbid();

            message.IsRead = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            if (!CanAccess(message)) return Forbid();

            _context.ContactMessages.Remove(message);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("thread/{carId:int}/{otherUserId}")]
        public async Task<ActionResult<IEnumerable<ContactMessageResponseDto>>> GetConversationThread(int carId, string otherUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var messages = await _context.ContactMessages
                .Include(m => m.Car)
                .Where(m => m.CarId == carId &&
                            ((m.SenderUserId == currentUserId && m.SellerUserId == otherUserId) ||
                             (m.SenderUserId == otherUserId && m.SellerUserId == currentUserId)))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(m => ToResponse(m, m.Car)).ToList();
        }

        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<object>>> GetActiveConversations()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var allUserMessages = await _context.ContactMessages
                .Include(m => m.Car)
                .Where(m => m.SenderUserId == currentUserId || m.SellerUserId == currentUserId)
                .ToListAsync();

            var conversations = allUserMessages
                .GroupBy(m => new
                {
                    m.CarId,
                    CarTitle = m.Car == null ? "Unknown Car" : $"{m.Car.Year} {m.Car.Make} {m.Car.Model}",
                    OtherUserId = m.SenderUserId == currentUserId ? m.SellerUserId : m.SenderUserId,
                    OtherUserName = m.SenderUserId == currentUserId ? "Seller" : m.SenderName
                })
                .Select(g => new
                {
                    g.Key.CarId,
                    g.Key.CarTitle,
                    g.Key.OtherUserId,
                    g.Key.OtherUserName,
                    LatestMessage = g.OrderByDescending(m => m.CreatedAt).First().Message,
                    LastMessageAt = g.Max(m => m.CreatedAt),
                    UnreadCount = g.Count(m => m.SellerUserId == currentUserId && !m.IsRead)
                })
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();

            return Ok(conversations);
        }

        private bool CanAccess(ContactMessage message)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return User.IsInRole("Admin") ||
                   message.SenderUserId == currentUserId ||
                   message.SellerUserId == currentUserId;
        }

        private static ContactMessageResponseDto ToResponse(ContactMessage message, Car? car)
        {
            return new ContactMessageResponseDto
            {
                Id = message.Id,
                CarId = message.CarId,
                CarTitle = car == null ? string.Empty : $"{car.Year} {car.Make} {car.Model}",
                SenderUserId = message.SenderUserId,
                SenderName = message.SenderName,
                SenderEmail = message.SenderEmail,
                SenderPhone = message.SenderPhone,
                SellerUserId = message.SellerUserId,
                RecipientUserId = message.SellerUserId,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead
            };
        }
    }
}
