using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;

        public WishlistController(ZoomiesDbContext context)
        {
            _context = context;
        }

        // GET: api/Wishlist
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> GetWishlist()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cars = await _context.WishlistItems
                .Where(item => item.UserId == currentUserId)
                .OrderByDescending(item => item.CreatedAt)
                .Include(item => item.Car)
                .Select(item => item.Car!)
                .ToListAsync();

            return cars.Select(car => new CarResponseDto
            {
                Id = car.Id,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                Mileage = car.Mileage,
                Price = car.Price,
                Transmission = car.Transmission,
                Condition = car.Condition,
                Category = car.Category,
                ImageUrl = car.ImageUrl,
                Description = car.Description,
                IsFeatured = car.IsFeatured,
                IsOwner = car.UserId == currentUserId,
                CanManage = car.UserId == currentUserId || User.IsInRole("Admin")
            }).ToList();
        }

        // POST: api/Wishlist/5
        [HttpPost("{carId}")]
        public async Task<IActionResult> AddToWishlist(int carId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var carExists = await _context.Cars.AnyAsync(car => car.Id == carId);
            if (!carExists) return NotFound("Car not found");

            var alreadySaved = await _context.WishlistItems
                .AnyAsync(item => item.UserId == currentUserId && item.CarId == carId);

            if (alreadySaved) return NoContent();

            _context.WishlistItems.Add(new WishlistItem
            {
                UserId = currentUserId,
                CarId = carId
            });

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Wishlist/5
        [HttpDelete("{carId}")]
        public async Task<IActionResult> RemoveFromWishlist(int carId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == currentUserId && w.CarId == carId);

            if (item == null) return NoContent();

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
