using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Zoomies.Controllers;
using Zoomies.Data;
using Zoomies.Hubs;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class ContactMessagesControllerTests
    {
        private static ZoomiesDbContext CreateDb()
        {
            var options = new DbContextOptionsBuilder<ZoomiesDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ZoomiesDbContext(options);
        }

        private static ContactMessagesController CreateController(
            ZoomiesDbContext db,
            string userId = "1",
            string userName = "Alice",
            string userEmail = "alice@test.com")
        {
            var controller = new ContactMessagesController(db, new NullHubContext());
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, userEmail)
            };
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
            return controller;
        }

        // ── CreateMessage ─────────────────────────────────────────

        [TestMethod]
        public async Task CreateMessage_ValidRequest_Returns201()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 1, Make = "Toyota", Model = "Corolla", Year = 2020, Price = 10000m, UserId = "2" });
            await db.SaveChangesAsync();

            var controller = CreateController(db, userId: "1");
            var dto = new ContactMessageCreateDto { CarId = 1, Message = "Is this car still available?" };

            var result = await controller.CreateMessage(dto);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult),
                "A valid message to another user's car should return 201 Created.");
        }

        [TestMethod]
        public async Task CreateMessage_CarNotFound_ReturnsNotFound()
        {
            using var db = CreateDb();
            var controller = CreateController(db, userId: "1");
            var dto = new ContactMessageCreateDto { CarId = 999, Message = "Hello" };

            var result = await controller.CreateMessage(dto);

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult),
                "Sending a message about a non-existent car should return 404.");
        }

        [TestMethod]
        public async Task CreateMessage_SelfMessage_ReturnsBadRequest()
        {
            using var db = CreateDb();
            // Car is owned by user "1" — same as the sender
            db.Cars.Add(new Car { Id = 2, Make = "BMW", Model = "M3", Year = 2022, Price = 50000m, UserId = "1" });
            await db.SaveChangesAsync();

            var controller = CreateController(db, userId: "1");
            var dto = new ContactMessageCreateDto { CarId = 2, Message = "Talking to myself" };

            var result = await controller.CreateMessage(dto);

            Assert.IsInstanceOfType(result.Result, typeof(BadRequestObjectResult),
                "Sending a message to yourself should return 400.");
        }

        // ── GetInbox ──────────────────────────────────────────────

        [TestMethod]
        public async Task GetInbox_ReturnsOnlyIncomingMessages()
        {
            using var db = CreateDb();
            db.Cars.Add(new Car { Id = 3, Make = "Ford", Model = "Focus", Year = 2019, Price = 12000m, UserId = "2" });
            db.ContactMessages.AddRange(
                // incoming — SellerUserId matches current user "1"
                new ContactMessage { CarId = 3, SenderUserId = "2", SenderName = "Bob", SenderEmail = "bob@test.com", SellerUserId = "1", Message = "For you" },
                // outgoing — this should NOT appear in "1"s inbox
                new ContactMessage { CarId = 3, SenderUserId = "1", SenderName = "Alice", SenderEmail = "alice@test.com", SellerUserId = "2", Message = "From you" }
            );
            await db.SaveChangesAsync();

            var controller = CreateController(db, userId: "1");
            var actionResult = await controller.GetInbox();

            var messages = actionResult.Value!.ToList();
            Assert.AreEqual(1, messages.Count, "Inbox should contain only messages addressed to the current user.");
            Assert.AreEqual("For you", messages[0].Message);
        }

        // ── SignalR stubs ─────────────────────────────────────────

        private sealed class NullClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class NullHubClients : IHubClients
        {
            private static readonly IClientProxy _proxy = new NullClientProxy();
            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Client(string connectionId) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
            public IClientProxy Group(string groupName) => _proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
            public IClientProxy User(string userId) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        }

        private sealed class NullHubContext : IHubContext<ChatHub>
        {
            public IHubClients Clients => new NullHubClients();
            public IGroupManager Groups => null!;
        }
    }
}
