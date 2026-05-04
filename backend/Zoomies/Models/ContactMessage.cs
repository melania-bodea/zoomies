using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        [Required]
        public int CarId { get; set; }

        public Car? Car { get; set; }

        [Required]
        public string SenderUserId { get; set; } = string.Empty;

        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;

        public string SenderPhone { get; set; } = string.Empty;

        [Required]
        public string SellerUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }
    }

    public class ContactMessageCreateDto
    {
        [Required]
        public int CarId { get; set; }

        [Phone]
        public string SenderPhone { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;
    }

    public class ContactMessageResponseDto
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarTitle { get; set; } = string.Empty;
        public string SenderUserId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPhone { get; set; } = string.Empty;
        public string SellerUserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
