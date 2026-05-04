using Microsoft.AspNetCore.Mvc;
using Zoomies.Models;

namespace Zoomies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceEstimatesController : ControllerBase
    {
        [HttpPost]
        public ActionResult<PriceEstimationResponseDto> EstimatePrice(PriceEstimationRequestDto request)
        {
            var age = Math.Max(0, DateTime.UtcNow.Year - request.Year);
            var expectedMileage = age * 15000;
            var basePrice = request.BaseMarketPrice;

            var ageAdjustment = -basePrice * Math.Min(0.60m, age * 0.035m);
            var mileageAdjustment = CalculateMileageAdjustment(basePrice, request.Mileage, expectedMileage);
            var conditionAdjustment = basePrice * GetConditionMultiplier(request.Condition);
            var defectAdjustment = CalculateDefectAdjustment(basePrice, request.Defects);

            var estimatedPrice = basePrice + ageAdjustment + mileageAdjustment + conditionAdjustment + defectAdjustment;
            estimatedPrice = Math.Max(500m, Math.Round(estimatedPrice, 2));

            return Ok(new PriceEstimationResponseDto
            {
                Vehicle = $"{request.Year} {request.Make} {request.Model}",
                BaseMarketPrice = basePrice,
                EstimatedPrice = estimatedPrice,
                AgeAdjustment = Math.Round(ageAdjustment, 2),
                MileageAdjustment = Math.Round(mileageAdjustment, 2),
                ConditionAdjustment = Math.Round(conditionAdjustment, 2),
                DefectAdjustment = Math.Round(defectAdjustment, 2),
                Notes =
                {
                    "This is a transparent rule-based estimate, not a final sale price.",
                    "The frontend can show each adjustment so users understand why the price changed."
                }
            });
        }

        private static decimal CalculateMileageAdjustment(decimal basePrice, int mileage, int expectedMileage)
        {
            if (expectedMileage <= 0)
                return mileage == 0 ? basePrice * 0.05m : -basePrice * Math.Min(0.12m, mileage / 100000m * 0.10m);

            var mileageDelta = mileage - expectedMileage;
            if (mileageDelta < 0)
                return basePrice * Math.Min(0.08m, Math.Abs(mileageDelta) / 100000m * 0.08m);

            return -basePrice * Math.Min(0.25m, mileageDelta / 100000m * 0.10m);
        }

        private static decimal GetConditionMultiplier(string condition)
        {
            return condition.Trim().ToLowerInvariant() switch
            {
                "new" => 0.10m,
                "certified" => 0.07m,
                "restored" => 0.04m,
                "used" => 0m,
                "excellent" => 0.08m,
                "good" => 0m,
                "fair" => -0.10m,
                "poor" => -0.22m,
                _ => -0.05m
            };
        }

        private static decimal CalculateDefectAdjustment(decimal basePrice, IEnumerable<VehicleDefectDto> defects)
        {
            var totalRepairCost = defects.Sum(d => d.EstimatedRepairCost);
            var severityPenalty = defects.Sum(d => d.Severity) * basePrice * 0.01m;
            var cappedPenalty = Math.Min(basePrice * 0.35m, totalRepairCost + severityPenalty);
            return -cappedPenalty;
        }
    }
}
