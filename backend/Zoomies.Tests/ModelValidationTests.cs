using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class ModelValidationTests
    {
        // Helper: runs validation on any object, returns list of errors
        private static List<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, validateAllProperties: true);
            return results;
        }

        // ── Car validation ───────────────────────────────────────

        [TestMethod]
        public void Car_ValidData_PassesValidation()
        {
            var car = new Car
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = 2020,
                Mileage = 50000,
                Price = 8000m,
                UserId = "1"
            };
            Assert.AreEqual(0, Validate(car).Count, "A valid car should have no validation errors.");
        }

        [TestMethod]
        public void Car_MissingMake_FailsValidation()
        {
            var car = new Car { Make = "", Model = "Corolla", Year = 2020, Price = 5000m };
            var errors = Validate(car);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Make")),
                "Empty Make should fail validation.");
        }

        [TestMethod]
        public void Car_YearTooOld_FailsValidation()
        {
            var car = new Car { Make = "Ford", Model = "T", Year = 1800, Price = 5000m };
            var errors = Validate(car);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Year")),
                "Year before 1886 should fail validation.");
        }

        [TestMethod]
        public void Car_YearInFuture_FailsValidation()
        {
            var car = new Car { Make = "Ford", Model = "Future", Year = 2099, Price = 5000m };
            var errors = Validate(car);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Year")),
                "Year beyond 2026 should fail validation.");
        }

        [TestMethod]
        public void Car_NegativeMileage_FailsValidation()
        {
            var car = new Car { Make = "BMW", Model = "M3", Year = 2020, Mileage = -1, Price = 5000m };
            var errors = Validate(car);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Mileage")),
                "Negative mileage should fail validation.");
        }

        // ── UserRegisterDto validation ───────────────────────────

        [TestMethod]
        public void UserRegisterDto_ValidData_PassesValidation()
        {
            var dto = new UserRegisterDto
            {
                Name = "John",
                Email = "john@test.com",
                PhoneNumber = "+40700000001",
                Password = "Secret123"
            };
            Assert.AreEqual(0, Validate(dto).Count, "Valid registration data should pass.");
        }

        [TestMethod]
        public void UserRegisterDto_InvalidEmail_FailsValidation()
        {
            var dto = new UserRegisterDto
            {
                Name = "John",
                Email = "notanemail",
                PhoneNumber = "+40700000001",
                Password = "Secret123"
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Email")),
                "Invalid email format should fail validation.");
        }

        [TestMethod]
        public void UserRegisterDto_ShortPassword_FailsValidation()
        {
            var dto = new UserRegisterDto
            {
                Name = "John",
                Email = "john@test.com",
                PhoneNumber = "+40700000001",
                Password = "123"
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Password")),
                "Password under 6 characters should fail validation.");
        }

        [TestMethod]
        public void UserRegisterDto_MissingName_FailsValidation()
        {
            var dto = new UserRegisterDto
            {
                Name = "",
                Email = "john@test.com",
                PhoneNumber = "+40700000001",
                Password = "Secret123"
            };
            var errors = Validate(dto);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Name")),
                "Empty name should fail validation.");
        }

        // ── ContactMessage validation ────────────────────────────

        [TestMethod]
        public void ContactMessage_ValidData_PassesValidation()
        {
            var msg = new ContactMessage
            {
                CarId = 1,
                SenderUserId = "1",
                SenderName = "John",
                SenderEmail = "john@test.com",
                SellerUserId = "2",
                Message = "Is this car still available?"
            };
            Assert.AreEqual(0, Validate(msg).Count, "Valid contact message should pass.");
        }

        [TestMethod]
        public void ContactMessage_EmptyMessage_FailsValidation()
        {
            var msg = new ContactMessage
            {
                CarId = 1,
                SenderUserId = "1",
                SenderName = "John",
                SenderEmail = "john@test.com",
                SellerUserId = "2",
                Message = ""
            };
            var errors = Validate(msg);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("Message")),
                "Empty message body should fail validation.");
        }

        [TestMethod]
        public void ContactMessage_InvalidSenderEmail_FailsValidation()
        {
            var msg = new ContactMessage
            {
                CarId = 1,
                SenderUserId = "1",
                SenderName = "John",
                SenderEmail = "notanemail",
                SellerUserId = "2",
                Message = "Hello"
            };
            var errors = Validate(msg);
            Assert.IsTrue(errors.Any(e => e.MemberNames.Contains("SenderEmail")),
                "Invalid sender email should fail validation.");
        }
    }
}
