using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zoomies.Controllers;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class WishlistControllerTests
    {
        private static ZoomiesDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ZoomiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ZoomiesDbContext(options);
        }

        private static WishlistController CreateController(ZoomiesDbContext db, string userId = "1")
        {
            var controller = new WishlistController(db);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
            return controller;
        }

        // ── AddToWishlist ─────────────────────────────────────────

        [TestMethod]
        public async Task AddToWishlist_ExistingCar_ReturnsNoContent()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 10, Make = "Toyota", Model = "Corolla", Year = 2020, Price = 10000m, UserId = "2" });
            await db.SaveChangesAsync();

            var controller = CreateController(db, "1");
            var result = await controller.AddToWishlist(10);

            Assert.IsInstanceOfType(result, typeof(NoContentResult),
                "Adding an existing car should return 204 and create a wishlist entry.");
            Assert.AreEqual(1, db.WishlistItems.Count());
        }

        [TestMethod]
        public async Task AddToWishlist_NonExistentCar_ReturnsNotFound()
        {
            using var db = CreateDb();
            var controller = CreateController(db, "1");

            var result = await controller.AddToWishlist(999);

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult),
                "Adding a car that doesn't exist should return 404.");
        }

        [TestMethod]
        public async Task AddToWishlist_AlreadySaved_ReturnsNoContentWithoutDuplicate()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 11, Make = "BMW", Model = "M3", Year = 2021, Price = 50000m, UserId = "2" });
            db.WishlistItems.Add(new WishlistItem { UserId = "1", CarId = 11 });
            await db.SaveChangesAsync();

            var controller = CreateController(db, "1");
            var result = await controller.AddToWishlist(11);

            Assert.IsInstanceOfType(result, typeof(NoContentResult),
                "Adding a car already in the wishlist should still return 204.");
            Assert.AreEqual(1, db.WishlistItems.Count(), "No duplicate entry should be created.");
        }

        // ── RemoveFromWishlist ────────────────────────────────────

        [TestMethod]
        public async Task RemoveFromWishlist_ExistingItem_ReturnsNoContent()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 12, Make = "Audi", Model = "A4", Year = 2019, Price = 30000m, UserId = "2" });
            db.WishlistItems.Add(new WishlistItem { UserId = "1", CarId = 12 });
            await db.SaveChangesAsync();

            var controller = CreateController(db, "1");
            var result = await controller.RemoveFromWishlist(12);

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
            Assert.AreEqual(0, db.WishlistItems.Count(), "Item should be removed from the wishlist.");
        }

        // ── GetWishlist ───────────────────────────────────────────

        [TestMethod]
        public async Task GetWishlist_ReturnsOnlyCurrentUserCars()
        {
            using var db = CreateDb();
            db.Cars.AddRange(
                new Car { Id = 20, Make = "Ford", Model = "Focus", Year = 2018, Price = 12000m, UserId = "2" },
                new Car { Id = 21, Make = "VW", Model = "Golf", Year = 2019, Price = 15000m, UserId = "2" }
            );
            db.WishlistItems.Add(new WishlistItem { UserId = "1", CarId = 20 }); // user "1" saved only car 20
            await db.SaveChangesAsync();

            var controller = CreateController(db, "1");
            var actionResult = await controller.GetWishlist();

            var items = actionResult.Value!.ToList();
            Assert.AreEqual(1, items.Count, "Wishlist should contain only the car saved by this user.");
            Assert.AreEqual("Ford", items[0].Make);
        }
    }
}
