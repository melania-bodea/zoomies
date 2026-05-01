using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zoomies.Data;
using Zoomies.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Zoomies.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ZoomiesDbContext _context;
        private readonly IConfiguration _configuration;

        // Constructor: Grabs the database and the AppSettings (where your secret key is)
        public AuthController(ZoomiesDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // ============================================================
        // 1. REGISTER: Creating a new user
        // ============================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto request)
        {
            // Check if email is already taken before doing anything else
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email already in use");

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                // SECURITY: Scrambles the password so it's unreadable in the DB
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("Account created successfully");
        }

        // ============================================================
        // 2. LOGIN: Validating credentials and issuing "Keys"
        // ============================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto request)
        {
            // Find the user by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            // Compare the typed password with the scrambled hash in the DB
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return BadRequest("Invalid credentials");

            // Create the short-term JWT access token
            string token = CreateToken(user);

            // Create a long-term Refresh Token to keep them logged in
            var refreshToken = GenerateRefreshToken();
            await SetRefreshToken(refreshToken, user);

            return Ok(new { token = token });
        }

        // ============================================================
        // 3. REFRESH TOKEN: Get a new JWT without a password
        // ============================================================
        /// <summary>
        /// When the 15-minute JWT expires, the frontend calls this to get a new one
        /// using the 'refreshToken' stored in a secure cookie.
        /// </summary>
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken()
        {
            // Grab the token from the hidden browser cookie
            var refreshToken = Request.Cookies["refreshToken"];
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            // Security check: Does the token exist and is it still valid?
            if (user == null || user.TokenExpires < DateTime.Now)
                return Unauthorized("Token expired or invalid.");

            // Give them a fresh JWT and a fresh Refresh Token (Rotating tokens)
            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken();
            await SetRefreshToken(newRefreshToken, user);

            return Ok(new { token = token });
        }

        // ============================================================
        // HELPER METHODS (The "behind the scenes" logic)
        // ============================================================

        /// <summary>
        /// Bakes the User's ID, Role, and Name into a signed JWT string.
        /// </summary>
        private string CreateToken(User user)
        {
            // Claims are "Facts" about the user baked into the token
            var claims = new List<Claim> {
               new Claim(ClaimTypes.Name, user.Name),
               new Claim(ClaimTypes.Email, user.Email),
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Role, user.Role) // CRITICAL for Admin logic
            };

            // Sign the token using your secret key from appsettings.json
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(15), // How long until they need a refresh
                    signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Creates a random, high-entropy string for the refresh token.
        /// </summary>
        private string GenerateRefreshToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }

        /// <summary>
        /// Saves the refresh token to the DB and sends it to the browser as a secure cookie.
        /// </summary>
        private async Task SetRefreshToken(string newRefreshToken, User user)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevents JavaScript from stealing the token
                Expires = DateTime.Now.AddDays(7), // User stays logged in for 7 days
                Secure = true, // Only sends over HTTPS
                SameSite = SameSiteMode.None
            };

            // Attach the cookie to the response
            Response.Cookies.Append("refreshToken", newRefreshToken, cookieOptions);

            // Sync the database with the new token details
            user.RefreshToken = newRefreshToken;
            user.TokenCreated = DateTime.Now;
            user.TokenExpires = cookieOptions.Expires.Value.DateTime;

            await _context.SaveChangesAsync();
        }
    }
}