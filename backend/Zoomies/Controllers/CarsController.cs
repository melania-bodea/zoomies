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
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> GetCars(
            string? search = null,
            string? make = null,
            string? model = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minYear = null,
            int? maxYear = null,
            int? maxMileage = null,
            string? transmission = null,
            string? category = null,
            bool? featuredOnly = null,
            string? sortBy = null,
            string sortDirection = "asc")
        {
            var query = _context.Cars.AsQueryable();

            // Filter logic: Only add to the query if the user provided search terms
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                var pattern = $"%{searchTerm}%";
                if (int.TryParse(searchTerm, out var searchYear))
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.Make, pattern) ||
                        EF.Functions.Like(c.Model, pattern) ||
                        c.Year == searchYear);
                }
                else
                {
                    query = query.Where(c =>
                        EF.Functions.Like(c.Make, pattern) ||
                        EF.Functions.Like(c.Model, pattern));
                }
            }

            if (!string.IsNullOrEmpty(make))
                query = query.Where(c => EF.Functions.Like(c.Make, $"%{make.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(model))
                query = query.Where(c => EF.Functions.Like(c.Model, $"%{model.Trim()}%"));

            if (minPrice.HasValue)
                query = query.Where(c => c.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            if (minYear.HasValue)
                query = query.Where(c => c.Year >= minYear.Value);

            if (maxYear.HasValue)
                query = query.Where(c => c.Year <= maxYear.Value);

            if (maxMileage.HasValue)
                query = query.Where(c => c.Mileage <= maxMileage.Value);

            if (!string.IsNullOrWhiteSpace(transmission))
                query = query.Where(c => EF.Functions.Like(c.Transmission, $"%{transmission.Trim()}%"));

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(c => EF.Functions.Like(c.Category, $"%{category.Trim()}%"));

            if (featuredOnly == true)
                query = query.Where(c => c.IsFeatured);

            var descending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
            query = sortBy?.ToLowerInvariant() switch
            {
                "price" => descending ? query.OrderByDescending(c => c.Price) : query.OrderBy(c => c.Price),
                "year" => descending ? query.OrderByDescending(c => c.Year) : query.OrderBy(c => c.Year),
                "mileage" => descending ? query.OrderByDescending(c => c.Mileage) : query.OrderBy(c => c.Mileage),
                "make" => descending ? query.OrderByDescending(c => c.Make) : query.OrderBy(c => c.Make),
                _ => query.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => c.Year)
            };

            var cars = await query.ToListAsync();
            return cars.Select(ToResponseDto).ToList();
        }

        // GET: api/Cars/5 (PUBLIC)
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CarResponseDto>> GetCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return ToResponseDto(car);
        }

        // GET: api/Cars/mine (LOGGED-IN USER ONLY)
        [Authorize]
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> GetMyCars()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cars = await _context.Cars
                .Where(c => c.UserId == currentUserId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            return cars.Select(ToResponseDto).ToList();
        }

        // ============================================================
        // POST: api/Cars (SECURE)
        // ============================================================
        /// <summary>
        /// Creates a new car. The [Authorize] tag ensures only logged-in users enter.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CarResponseDto>> PostCar(CarMutateDto request)
        {
            // SECURITY: Extract the User ID from the JWT Token.
            // We 'force' the car to belong to the logged-in user, ignoring what's in the JSON body.
            var car = new Car
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                Make = request.Make,
                Model = request.Model,
                Year = request.Year,
                Mileage = request.Mileage,
                Price = request.Price!.Value,
                Transmission = request.Transmission,
                Condition = request.Condition,
                Category = request.Category,
                ImageUrl = request.ImageUrl,
                Description = request.Description
            };

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            // Returns a 201 Created status and the location of the new car
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, ToResponseDto(car));
        }

        // ============================================================
        // PUT: api/Cars/5 (OWNER OR ADMIN ONLY)
        // ============================================================
        /// <summary>
        /// Updates an existing car. Includes a strict ownership check.
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCar(int id, CarMutateDto request)
        {
            var existingCar = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id);
            if (existingCar == null) return NotFound();

            // --- THE SECURITY GATE ---
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAdmin = User.IsInRole("Admin");

            // Logic: If you are NOT the owner AND you are NOT an admin, you are kicked out.
            if (existingCar.UserId != currentUserId && !isAdmin)
            {
                return Forbid(); // 403 Forbidden
            }

            existingCar.Make = request.Make;
            existingCar.Model = request.Model;
            existingCar.Year = request.Year;
            existingCar.Mileage = request.Mileage;
            existingCar.Price = request.Price!.Value;
            existingCar.Transmission = request.Transmission;
            existingCar.Condition = request.Condition;
            existingCar.Category = request.Category;
            existingCar.ImageUrl = request.ImageUrl;
            existingCar.Description = request.Description;

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

        private CarResponseDto ToResponseDto(Car car)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isOwner = !string.IsNullOrEmpty(currentUserId) && car.UserId == currentUserId;
            var canManage = isOwner || User.IsInRole("Admin");

            return new CarResponseDto
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
                IsOwner = isOwner,
                CanManage = canManage
            };
        }
    }
}
