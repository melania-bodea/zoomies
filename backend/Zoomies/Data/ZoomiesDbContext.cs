using Microsoft.EntityFrameworkCore;
using Zoomies.Models;

namespace Zoomies.Data
{
    public class ZoomiesDbContext : DbContext
    {
        public ZoomiesDbContext(DbContextOptions<ZoomiesDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed Cars
            modelBuilder.Entity<Car>().HasData(
                new Car { Id = 1, Make = "Hyundai", Model = "Accent III", Year = 2008, Price = 100000, Mileage = 200000, Transmission = "Manual", IsFeatured = true, ImageUrl = "...", UserId = "1" },
                new Car { Id = 2, Make = "BMW", Model = "M3", Year = 2022, Price = 75000, Mileage = 12000, Transmission = "Automatic", IsFeatured = true, ImageUrl = "...", UserId = "2" }
            );

            // Seed an Admin User so you don't have to manually edit the DB
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@zoomies.com",
                    PasswordHash = "$2a$10$darOjZodJy21dQK2lira5eVywMpxr3Xb4cXBf7kL7DVfXdouQwiS.",// Password is "Admin123!"
                    Role = "Admin" // <--- This gives User 1 super powers
                }
            );
        }

        // This property represents the table in your database
        public DbSet<Car> Cars { get; set; }
        public DbSet<User> Users { get; set; }
    }
}