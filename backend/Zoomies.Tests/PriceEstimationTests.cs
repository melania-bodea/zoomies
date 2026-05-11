using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Zoomies.Controllers;
using Zoomies.Models;

namespace Zoomies.Tests
{
    [TestClass]
    public class PriceEstimationTests
    {
        private PriceEstimatesController _controller;

        [TestInitialize]
        public void Setup()
        {
            _controller = new PriceEstimatesController();
        }

        // ── Condition adjustments ────────────────────────────────

        [TestMethod]
        public void EstimatePrice_NewCondition_IncreasesPrice()
        {
            var request = MakeRequest(condition: "New");
            var result = GetDto(request);
            Assert.IsTrue(result.EstimatedPrice > request.BaseMarketPrice,
                "A 'New' car should be priced above base market price.");
        }

        [TestMethod]
        public void EstimatePrice_PoorCondition_DecreasesPrice()
        {
            var request = MakeRequest(condition: "Poor");
            var result = GetDto(request);
            Assert.IsTrue(result.EstimatedPrice < request.BaseMarketPrice,
                "A 'Poor' condition car should be priced below base market price.");
        }

        [TestMethod]
        public void EstimatePrice_UsedCondition_NoConditionAdjustment()
        {
            var request = MakeRequest(condition: "Used");
            var result = GetDto(request);
            Assert.AreEqual(0m, result.ConditionAdjustment,
                "Condition='Used' should produce zero condition adjustment.");
        }

        // ── Minimum price floor ──────────────────────────────────

        [TestMethod]
        public void EstimatePrice_VeryOldBadCar_NeverGoesBelowMinimum()
        {
            var request = new PriceEstimationRequestDto
            {
                Make = "Test",
                Model = "Car",
                Year = 1990,
                Mileage = 999999,
                BaseMarketPrice = 600m,
                Condition = "Poor",
                Defects = new List<VehicleDefectDto>
                {
                    new() { Name = "Engine failure", Severity = 5, EstimatedRepairCost = 10000m }
                }
            };
            var result = GetDto(request);
            Assert.IsTrue(result.EstimatedPrice >= 500m,
                "Estimated price should never drop below the $500 floor.");
        }

        // ── Age adjustment ───────────────────────────────────────

        [TestMethod]
        public void EstimatePrice_OlderCar_HasNegativeAgeAdjustment()
        {
            var request = MakeRequest(year: 2000);
            var result = GetDto(request);
            Assert.IsTrue(result.AgeAdjustment < 0,
                "An older car should have a negative age adjustment.");
        }

        [TestMethod]
        public void EstimatePrice_CurrentYearCar_AgeAdjustmentNearZero()
        {
            var request = MakeRequest(year: DateTime.UtcNow.Year);
            var result = GetDto(request);
            Assert.AreEqual(0m, result.AgeAdjustment,
                "A brand new car should have zero age adjustment.");
        }

        // ── Defects ──────────────────────────────────────────────

        [TestMethod]
        public void EstimatePrice_WithDefects_LowersTotalPrice()
        {
            var withoutDefects = GetDto(MakeRequest());
            var withDefects = GetDto(new PriceEstimationRequestDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = DateTime.UtcNow.Year,
                Mileage = 15000,
                BaseMarketPrice = 10000m,
                Condition = "Used",
                Defects = new List<VehicleDefectDto>
                {
                    new() { Name = "Cracked bumper", Severity = 2, EstimatedRepairCost = 500m }
                }
            });
            Assert.IsTrue(withDefects.EstimatedPrice < withoutDefects.EstimatedPrice,
                "Adding defects should lower the estimated price.");
        }

        [TestMethod]
        public void EstimatePrice_NoDefects_ZeroDefectAdjustment()
        {
            var request = MakeRequest();
            var result = GetDto(request);
            Assert.AreEqual(0m, result.DefectAdjustment,
                "No defects should produce zero defect adjustment.");
        }

        // ── Response structure ───────────────────────────────────

        [TestMethod]
        public void EstimatePrice_ResponseContainsVehicleString()
        {
            var request = MakeRequest();
            var result = GetDto(request);
            StringAssert.Contains(result.Vehicle, request.Make);
            StringAssert.Contains(result.Vehicle, request.Model);
        }

        [TestMethod]
        public void EstimatePrice_ResponseContainsNotes()
        {
            var request = MakeRequest();
            var result = GetDto(request);
            Assert.IsTrue(result.Notes.Count > 0,
                "Response should always include explanation notes.");
        }

        // ── Helpers ──────────────────────────────────────────────

        private static PriceEstimationRequestDto MakeRequest(
            string condition = "Used",
            int year = 0,
            int mileage = 15000,
            decimal basePrice = 10000m)
        {
            return new PriceEstimationRequestDto
            {
                Make = "Toyota",
                Model = "Corolla",
                Year = year == 0 ? DateTime.UtcNow.Year : year,
                Mileage = mileage,
                BaseMarketPrice = basePrice,
                Condition = condition,
                Defects = new List<VehicleDefectDto>()
            };
        }

        private PriceEstimationResponseDto GetDto(PriceEstimationRequestDto request)
        {
            var actionResult = _controller.EstimatePrice(request);
            var okResult = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(okResult, "Controller should return 200 OK.");
            return (PriceEstimationResponseDto)okResult.Value!;
        }
    }
}