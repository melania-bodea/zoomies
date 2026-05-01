using System.ComponentModel.DataAnnotations;

namespace Zoomies.Models
{
    // ============================================================
    // 1. THE DATABASE ENTITY (The "Permanent Record")
    // ============================================================
    /// <summary>
    /// This represents the 'Users' table in SQL Server.
    /// It contains sensitive info like the PasswordHash and RefreshToken.
    /// </summary>
    public class User
    {
        // Primary Key for the database
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // SECURITY: We never store actual passwords. We store the Bcrypt hash.
        public string PasswordHash { get; set; } = string.Empty;

        // SECURITY: Used to give the user a new JWT without them logging in again
        public string RefreshToken { get; set; } = string.Empty;

        // Tracking when the refresh token was made and when it dies
        public DateTime TokenCreated { get; set; }
        public DateTime TokenExpires { get; set; }

        // PERMISSIONS: Determines if the user is a standard "User" or an "Admin"
        // Defaults to "User" to prevent unauthorized admin creation.
        public string Role { get; set; } = "User";
    }

    // ============================================================
    // 2. THE REGISTRATION DTO (The "Signup Form")
    // ============================================================
    /// <summary>
    /// DTO stands for Data Transfer Object. 
    /// This is what the user sends us when they want to create an account.
    /// It includes validation logic to ensure data is clean.
    /// </summary>
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")] // Ensures '@' and '.' exist
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        // SECURITY: Minimum length protects against easy-to-guess passwords
        [MinLength(6, ErrorMessage = "Password too weak. Must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;
    }

    // ============================================================
    // 3. THE LOGIN DTO (The "Keycard Request")
    // ============================================================
    /// <summary>
    /// This is a slimmed-down model used only for logging in.
    /// We don't need 'Name' here, just the credentials.
    /// </summary>
    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}