using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoomies.Data;
using Zoomies.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Zoomies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarsController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;

        // Dependency Injection: Bringing in the Database context
        public CarsController(ZoomiesDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET: api/Cars (PUBLIC)
        // ============================================================
        /// <summary>
        /// Allows anyone (logged in or not) to view the car list.
        /// Includes optional filtering by make and maximum price.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Car>>> GetCars(string? make = null, decimal? maxPrice = null)
        {
            var query = _context.Cars.AsQueryable();

            // Filter logic: Only add to the query if the user provided search terms
            if (!string.IsNullOrEmpty(make))
                query = query.Where(c => c.Make.Contains(make));

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            return await query.ToListAsync();
        }

        // GET: api/Cars/5 (PUBLIC)
        [HttpGet("{id}")]
        public async Task<ActionResult<Car>> GetCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return car;
        }

        // ============================================================
        // POST: api/Cars (SECURE)
        // ============================================================
        /// <summary>
        /// Creates a new car. The [Authorize] tag ensures only logged-in users enter.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Car>> PostCar(Car car)
        {
            // SECURITY: Extract the User ID from the JWT Token.
            // We 'force' the car to belong to the logged-in user, ignoring what's in the JSON body.
            car.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status and the location of the new car
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, car);
        }

        // ============================================================
        // PUT: api/Cars/5 (OWNER OR ADMIN ONLY)
        // ============================================================
        /// <summary>
        /// Updates an existing car. Includes a strict ownership check.
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCar(int id, Car car)
        {
            if (id != car.Id) return BadRequest();

            // AsNoTracking() is used here because we just want to 'check' the owner 
            // without the database manager trying to 'track' this specific object yet.
            var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (existingCar == null) return NotFound();

            // --- THE SECURITY GATE ---
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            // Logic: If you are NOT the owner AND you are NOT an admin, you are kicked out.
            if (existingCar.UserId != currentUserId && !isAdmin)
            {
                return Forbid(); // 403 Forbidden
            }

            // Ensure the UserId stays tied to the original owner even if changed in the request
            car.UserId = existingCar.UserId;

            _context.Entry(car).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ============================================================
        // DELETE: api/Cars/5 (OWNER OR ADMIN ONLY)
        // ============================================================
        /// <summary>
        /// Removes a car from the database. Owner and Admin only.
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            // --- THE SECURITY GATE ---
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            // Allow the action only if the User ID matches the car, OR if the user is an Admin
            if (car.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}