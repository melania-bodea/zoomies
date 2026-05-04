using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    public class PriceEstimationRequestDto
    {
        [Required]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1886, 2026)]
        public int Year { get; set; }

        [Range(0, 1000000)]
        public int Mileage { get; set; }

        [Range(1, 10000000)]
        public decimal BaseMarketPrice { get; set; }

        [Required]
        public string Condition { get; set; } = "Good";

        public List<VehicleDefectDto> Defects { get; set; } = new();
    }

    public class VehicleDefectDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Severity { get; set; } = 1;

        [Range(0, 1000000)]
        public decimal EstimatedRepairCost { get; set; }
    }

    public class PriceEstimationResponseDto
    {
        public string Vehicle { get; set; } = string.Empty;
        public decimal BaseMarketPrice { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal AgeAdjustment { get; set; }
        public decimal MileageAdjustment { get; set; }
        public decimal ConditionAdjustment { get; set; }
        public decimal DefectAdjustment { get; set; }
        public List<string> Notes { get; set; } = new();
    }
}
