using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
using Zoomies.Controllers;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class CarsControllerTests
    {
        private static ZoomiesDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ZoomiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ZoomiesDbContext(options);
        }

        private static CarsController CreateController(ZoomiesDbContext db, string userId = "1", bool isAdmin = false)
        {
            var controller = new CarsController(db, new FakeEnvironment());
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (isAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
            return controller;
        }

        // ── GetCar ────────────────────────────────────────────────

        [TestMethod]
        public async Task GetCar_ExistingId_ReturnsCarDto()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2020, Price = 10000m, UserId = "2" });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var result = await controller.GetCar(1);

            Assert.IsNotNull(result.Value, "GetCar with a valid id should return a car DTO.");
            Assert.AreEqual("Toyota", result.Value.Make);
        }

        [TestMethod]
        public async Task GetCar_NonExistentId_ReturnsNotFound()
        {
            using var db = CreateDb();
            var controller = CreateController(db);

            var result = await controller.GetCar(999);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult),
                "GetCar with an unknown id should return 404.");
        }

        // ── DeleteCar ─────────────────────────────────────────────

        [TestMethod]
        public async Task DeleteCar_Owner_ReturnsNoContent()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 5, Make = "BMW", Model = "M3", Year = 2022, Price = 50000m, UserId = "1" });
            await db.SaveChangesAsync();

            var controller = CreateController(db, userId: "1");
            var result = await controller.DeleteCar(5);

            Assert.IsInstanceOfType(result, typeof(NoContentResult),
                "Owner deleting their own car should return 204.");
            Assert.AreEqual(0, db.Cars.Count(), "Car should be removed from the database.");
        }

        [TestMethod]
        public async Task DeleteCar_NotOwner_ReturnsForbid()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 6, Make = "Audi", Model = "A4", Year = 2021, Price = 35000m, UserId = "2" });
            await db.SaveChangesAsync();

            var controller = CreateController(db, userId: "1"); // user "1" does not own car owned by "2"
            var result = await controller.DeleteCar(6);

            Assert.IsInstanceOfType(result, typeof(ForbidResult),
                "A non-owner trying to delete a car should be forbidden.");
        }

        // ── Fake IWebHostEnvironment (image upload paths not exercised here) ──

        private sealed class FakeEnvironment : IWebHostEnvironment
        {
            public string WebRootPath { get; set; } = Path.GetTempPath();
            public IFileProvider WebRootFileProvider { get; set; } = null!;
            public string ApplicationName { get; set; } = "Test";
            public IFileProvider ContentRootFileProvider { get; set; } = null!;
            public string ContentRootPath { get; set; } = Path.GetTempPath();
            public string EnvironmentName { get; set; } = "Test";
        }
    }
}
