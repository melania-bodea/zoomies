using System.ComponentModel.DataAnnotations.Schema;

namespace Zoomies.Models
{
    public class CarImage
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        [ForeignKey("CarId")]
        public Car? Car { get; set; }
    }
}
