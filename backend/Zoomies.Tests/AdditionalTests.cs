using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class AdditionalTests
    {
        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        // ── PagedResult math ─────────────────────────────────────

        [TestMethod]
        public void PagedResult_TotalPages_CalculatesCorrectly()
        {
            var paged = new PagedResult<Car> { TotalCount = 50, PageSize = 20 };
            Assert.AreEqual(3, paged.TotalPages,
                "50 items / 20 per page = 3 pages.");
        }

        [TestMethod]
        public void PagedResult_ExactFit_NoExtraPage()
        {
            var paged = new PagedResult<Car> { TotalCount = 40, PageSize = 20 };
            Assert.AreEqual(2, paged.TotalPages,
                "40 items / 20 per page = exactly 2 pages, no extra.");
        }

        [TestMethod]
        public void PagedResult_SingleItem_IsOnePage()
        {
            var paged = new PagedResult<Car> { TotalCount = 1, PageSize = 20 };
            Assert.AreEqual(1, paged.TotalPages,
                "1 item should still be 1 page.");
        }

        [TestMethod]
        public void PagedResult_ZeroItems_IsZeroPages()
        {
            var paged = new PagedResult<Car> { TotalCount = 0, PageSize = 20 };
            Assert.AreEqual(0, paged.TotalPages,
                "0 items should produce 0 pages.");
        }

        // ── CarMutateDto validation ──────────────────────────────

        [TestMethod]
        public void CarMutateDto_ValidData_PassesValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = 8000m
            };
            Assert.AreEqual(0, Validate(dto).Count,
                "Valid car data should pass all validation.");
        }

        [TestMethod]
        public void CarMutateDto_ZeroPrice_FailsValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = 0m
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Price")),
                "Price of 0 should fail — must be at least 1.");
        }

        [TestMethod]
        public void CarMutateDto_NegativePrice_FailsValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = -100m
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Price")),
                "Negative price should fail validation.");
        }

        [TestMethod]
        public void CarMutateDto_MissingMake_FailsValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = 5000m
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Make")),
                "Empty Make should fail validation.");
        }

        [TestMethod]
        public void CarMutateDto_YearTooOld_FailsValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "Ford",
                Model = "T",
                Year = 1800,
                Mileage = 0,
                Price = 5000m
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Year")),
                "Year before 1886 should fail validation.");
        }

        [TestMethod]
        public void CarMutateDto_NullPrice_FailsValidation()
        {
            var dto = new CarMutateDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = null
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Price")),
                "Null price should fail — price is required.");
        }

        // ── WishlistItem defaults ────────────────────────────────

        [TestMethod]
        public void WishlistItem_CreatedAt_DefaultsToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var item = new WishlistItem { UserId = "1", CarId = 1 };
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(item.CreatedAt >= before && item.CreatedAt <= after,
                "CreatedAt should default to approximately UtcNow.");
        }

        // ── ContactMessage defaults ──────────────────────────────

        [TestMethod]
        public void ContactMessage_CreatedAt_DefaultsToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var msg = new ContactMessage
            {
                CarId = 1,
                SenderUserId = "1",
                SenderName = "John",
                SenderEmail = "john@test.com",
                SellerUserId = "2",
                Message = "Hello"
            };
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(msg.CreatedAt >= before && msg.CreatedAt <= after,
                "CreatedAt should default to approximately UtcNow.");
        }

        [TestMethod]
        public void ContactMessage_IsRead_DefaultsFalse()
        {
            var msg = new ContactMessage();
            Assert.IsFalse(msg.IsRead,
                "New messages should default to unread.");
        }
    }
}