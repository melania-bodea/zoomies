using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    /// <summary>
    /// Data sent by the frontend when a user creates or edits a listing.
    /// It only contains fields that a normal user is allowed to control.
    /// </summary>
    public class CarMutateDto
    {
        [Required(ErrorMessage = "Make is required")]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required")]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Range(1886, 2027, ErrorMessage = "Please enter a valid year")]
        public int Year { get; set; }

        [Range(0, 1000000)]
        public int Mileage { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(1, 10000000, ErrorMessage = "Price must be a positive number")]
        public decimal? Price { get; set; }

        public string Transmission { get; set; } = "Automatic";

        public string Condition { get; set; } = "Used";

        public string Category { get; set; } = "Sports";

        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data returned to the frontend when cars are displayed.
    /// It includes simple permission flags instead of exposing internal ownership logic.
    /// </summary>
    public class CarResponseDto
    {
        public int Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Mileage { get; set; }
        public decimal Price { get; set; }
        public string Transmission { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFeatured { get; set; }
        public bool IsOwner { get; set; }
        public bool CanManage { get; set; }
    }
}
