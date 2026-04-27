using Microsoft.AspNetCore.Mvc;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CarsController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public IActionResult GetCars()
        {
            return Ok(new List<string> { "BMW", "Audi" });
        }
        [HttpPost]
        public IActionResult AddCar(Car car)
        {
            return Ok(car);
        }
    }
}
