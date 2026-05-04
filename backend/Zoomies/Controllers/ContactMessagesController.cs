using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ContactMessagesController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;

        public ContactMessagesController(ZoomiesDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<ContactMessageResponseDto>> CreateMessage(ContactMessageCreateDto request)
        {
            var car = await _context.Cars.FindAsync(request.CarId);
            if (car == null) return NotFound("Car listing not found.");

            var senderUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(senderUserId)) return Unauthorized();

            if (car.UserId == senderUserId)
                return BadRequest("You cannot contact yourself about your own listing.");

            var message = new ContactMessage
            {
                CarId = car.Id,
                SenderUserId = senderUserId,
                SenderName = User.FindFirstValue(ClaimTypes.Name) ?? "Unknown user",
                SenderEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                SenderPhone = request.SenderPhone,
                SellerUserId = car.UserId,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMessage), new { id = message.Id }, ToResponse(message, car));
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
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead
            };
        }
    }
}
