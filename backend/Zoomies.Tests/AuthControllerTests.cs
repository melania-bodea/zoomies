using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Zoomies.Controllers;
using Zoomies.Data;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class AuthControllerTests
    {
        private static ZoomiesDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ZoomiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ZoomiesDbContext(options);
        }

        private static IConfiguration CreateConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AppSettings:Token", "super-secret-test-key-that-is-long-enough-for-hmacsha512-signing-key!!" }
                })
                .Build();

        private static AuthController CreateController(ZoomiesDbContext db)
        {
            var controller = new AuthController(db, CreateConfig());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            return controller;
        }

        // ── Register ─────────────────────────────────────────────

        [TestMethod]
        public async Task Register_NewEmail_ReturnsOk()
        {
            using var db = CreateDb();
            var controller = CreateController(db);
            var dto = new UserRegisterDto
            {
                Name = "Alice",
                Email = "alice@test.com",
                PhoneNumber = "+40700000091",
                Password = "Password1"
            };

            var result = await controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult),
                "Registering with a new email should succeed with 200 OK.");
        }

        [TestMethod]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            using var db = CreateDb();
            db.Users.Add(new User { Name = "Bob", Email = "bob@test.com", PhoneNumber = "+40700000088", PasswordHash = "x", Role = "User" });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var dto = new UserRegisterDto { Name = "Bob2", Email = "bob@test.com", PhoneNumber = "+40700000087", Password = "Password1" };

            var result = await controller.Register(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult),
                "Registering with an already-used email should return 400.");
        }

        // ── Login ─────────────────────────────────────────────────

        [TestMethod]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            using var db = CreateDb();
            db.Users.Add(new User
            {
                Name = "Charlie",
                Email = "charlie@test.com",
                PhoneNumber = "+40700000077",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
                Role = "User"
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var dto = new UserLoginDto { Email = "charlie@test.com", Password = "Password1" };

            var result = await controller.Login(dto) as OkObjectResult;

            Assert.IsNotNull(result, "Valid credentials should return 200 OK.");
            Assert.IsNotNull(result.Value, "Response body should contain the token.");
        }

        [TestMethod]
        public async Task Login_WrongPassword_ReturnsBadRequest()
        {
            using var db = CreateDb();
            db.Users.Add(new User
            {
                Name = "Dave",
                Email = "dave@test.com",
                PhoneNumber = "+40700000066",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword"),
                Role = "User"
            });
            await db.SaveChangesAsync();

            var controller = CreateController(db);
            var dto = new UserLoginDto { Email = "dave@test.com", Password = "WrongPassword" };

            var result = await controller.Login(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult),
                "Wrong password should return 400.");
        }

        [TestMethod]
        public async Task Login_UnknownEmail_ReturnsBadRequest()
        {
            using var db = CreateDb();
            var controller = CreateController(db);
            var dto = new UserLoginDto { Email = "nobody@test.com", Password = "Password1" };

            var result = await controller.Login(dto);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult),
                "Login with an unknown email should return 400.");
        }
    }
}
