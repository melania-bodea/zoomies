using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    public class Car
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Make is required")]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1886, 2026, ErrorMessage = "Please enter a valid year")]
        public int Year { get; set; }

        [Range(0, 1000000)]
        public int Mileage { get; set; }

        [Range(1, 10000000)]
        public decimal Price { get; set; }

        public string Transmission { get; set; } = "Automatic";

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsFeatured { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
