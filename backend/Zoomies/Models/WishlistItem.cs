using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    /// <summary>
    /// One saved car for one logged-in user.
    /// The frontend uses this so wishlist data belongs to the account, not the browser.
    /// </summary>
    public class WishlistItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int CarId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Car? Car { get; set; }
    }
}
