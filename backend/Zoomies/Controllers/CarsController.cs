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
    public class CarsController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CarsController(ZoomiesDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CarResponseDto>>> GetCars(
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
            string sortDirection = "asc",
            int page = 1,
            int pageSize = 20)
        {
            var currentPage = Math.Max(1, page);
            var currentPageSize = Math.Clamp(pageSize, 1, 20);

            var query = _context.Cars
                .Include(car => car.Images)
                .AsQueryable();

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

            var totalCount = await query.CountAsync();
            var cars = await query
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var sellers = await LoadSellers(cars);
            return Ok(new PagedResult<CarResponseDto>
            {
                Items = cars.Select(car => ToResponseDto(car, sellers)).ToList(),
                TotalCount = totalCount,
                CurrentPage = currentPage,
                PageSize = currentPageSize
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CarResponseDto>> GetCar(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();
            var seller = await FindSeller(car.UserId);
            return ToResponseDto(car, seller);
        }

        [Authorize]
        [HttpGet("mine")]
        public async Task<ActionResult<IEnumerable<CarResponseDto>>> GetMyCars()
        {
            var currentUserId = GetCurrentUserId();

            var cars = await _context.Cars
                .Include(c => c.Images)
                .Where(c => c.UserId == currentUserId)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            var sellers = await LoadSellers(cars);
            return cars.Select(car => ToResponseDto(car, sellers)).ToList();
        }

        [Authorize]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CarResponseDto>> PostCar([FromForm] CarMutateDto request)
        {
            var car = new Car
            {
                UserId = GetCurrentUserId(),
                Make = request.Make,
                Model = request.Model,
                Year = request.Year,
                Mileage = request.Mileage,
                Price = request.Price!.Value,
                Transmission = request.Transmission,
                Condition = request.Condition,
                Category = request.Category,
                ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
                Description = request.Description,
                Images = new List<CarImage>()
            };

            var imageResult = await AddUploadedImages(car, request.Images);
            if (!imageResult.Success) return BadRequest(imageResult.Error);

            if (string.IsNullOrWhiteSpace(car.ImageUrl))
                return BadRequest("Please upload at least one image.");

            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            var seller = await FindSeller(car.UserId);
            return CreatedAtAction(nameof(GetCar), new { id = car.Id }, ToResponseDto(car, seller));
        }

        [Authorize]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PutCar(int id, [FromForm] CarMutateDto request)
        {
            var existingCar = await _context.Cars
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existingCar == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            if (existingCar.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            var imageResult = await AddUploadedImages(existingCar, request.Images);
            if (!imageResult.Success) return BadRequest(imageResult.Error);

            if (!string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                existingCar.ImageUrl = request.ImageUrl.Trim();
            }

            existingCar.Make = request.Make;
            existingCar.Model = request.Model;
            existingCar.Year = request.Year;
            existingCar.Mileage = request.Mileage;
            existingCar.Price = request.Price!.Value;
            existingCar.Transmission = request.Transmission;
            existingCar.Condition = request.Condition;
            existingCar.Category = request.Category;
            existingCar.Description = request.Description;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            bool isAdmin = User.IsInRole("Admin");

            if (car.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }

        private async Task<(bool Success, string? Error)> AddUploadedImages(Car car, List<IFormFile>? images)
        {
            if (images == null || images.Count == 0)
                return (true, null);

            foreach (var image in images)
            {
                var validationError = ValidateImage(image);
                if (validationError != null)
                    return (false, validationError);

                var imageUrl = await SaveImageLocally(image);
                car.Images.Add(new CarImage { ImageUrl = imageUrl });

                if (string.IsNullOrWhiteSpace(car.ImageUrl))
                {
                    car.ImageUrl = imageUrl;
                }
            }

            return (true, null);
        }

        private string? ValidateImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return "Image file is empty.";

            const long maxFileSize = 5 * 1024 * 1024;
            if (image.Length > maxFileSize)
                return "Image is too large. Maximum size is 5MB.";

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
                return "Invalid file type. Only JPG, PNG, and WEBP are allowed.";

            if (!image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return "The file content is not a valid image.";

            return null;
        }

        private async Task<string> SaveImageLocally(IFormFile image)
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "images", "cars");
            Directory.CreateDirectory(uploadsFolder);

            var safeFileName = Path.GetFileName(image.FileName);
            var uniqueFileName = $"{Guid.NewGuid():N}_{safeFileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(fileStream);

            return $"/images/cars/{uniqueFileName}";
        }

        private async Task<Dictionary<string, User>> LoadSellers(IEnumerable<Car> cars)
        {
            var sellerIds = cars
                .Select(car => int.TryParse(car.UserId, out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var sellers = await _context.Users
                .Where(user => sellerIds.Contains(user.Id))
                .ToListAsync();

            return sellers.ToDictionary(user => user.Id.ToString());
        }

        private async Task<User?> FindSeller(string userId)
        {
            if (!int.TryParse(userId, out var sellerId)) return null;
            return await _context.Users.FirstOrDefaultAsync(user => user.Id == sellerId);
        }

        private CarResponseDto ToResponseDto(Car car, IReadOnlyDictionary<string, User> sellers)
        {
            sellers.TryGetValue(car.UserId, out var seller);
            return ToResponseDto(car, seller);
        }

        private CarResponseDto ToResponseDto(Car car, User? seller)
        {
            var currentUserId = GetCurrentUserId();
            var isOwner = !string.IsNullOrEmpty(currentUserId) && car.UserId == currentUserId;
            var canManage = isOwner || User.IsInRole("Admin");
            var galleryUrls = car.Images?
                .OrderBy(image => image.Id)
                .Select(image => image.ImageUrl)
                .Where(url => !string.IsNullOrWhiteSpace(url))
                .Distinct()
                .ToList() ?? new List<string>();

            if (!string.IsNullOrWhiteSpace(car.ImageUrl))
            {
                galleryUrls.RemoveAll(url => url == car.ImageUrl);
                galleryUrls.Insert(0, car.ImageUrl);
            }

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
                GalleryUrls = galleryUrls,
                Description = car.Description,
                SellerName = seller?.Name ?? "Private Seller",
                SellerPhone = seller?.PhoneNumber ?? string.Empty,
                IsFeatured = car.IsFeatured,
                IsOwner = isOwner,
                CanManage = canManage
            };
        }
    }
}
