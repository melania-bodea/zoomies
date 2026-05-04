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
            modelBuilder.Entity<Car>()
                .Property(c => c.Price)
                .HasPrecision(18, 2);

            // Seed Cars
            modelBuilder.Entity<Car>().HasData(
                new Car
                {
                    Id = 1,
                    Make = "Hyundai",
                    Model = "Accent III",
                    Year = 2008,
                    Price = 100000,
                    Mileage = 200000,
                    Transmission = "Manual",
                    Condition = "Used",
                    Category = "Sports",
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=900",
                    Description = "Starter demo listing owned by the admin user.",
                    UserId = "1"
                },
                new Car
                {
                    Id = 2,
                    Make = "BMW",
                    Model = "M3",
                    Year = 2022,
                    Price = 75000,
                    Mileage = 12000,
                    Transmission = "Automatic",
                    Condition = "Used",
                    Category = "Sports",
                    IsFeatured = true,
                    ImageUrl = "https://images.unsplash.com/photo-1625231334168-35067f8853ed?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=900",
                    Description = "Demo seller listing used for buyer-to-seller contact tests.",
                    UserId = "2"
                }
            );

            // Seed an Admin User so you don't have to manually edit the DB
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Admin User",
                    Email = "admin@zoomies.com",
                    PasswordHash = "$2a$11$gqfJFPQzPs3HykWpu3P6z.fpG74WvoI5SBpYjKLyMvRxH57ESHQtq",// Password is "Admin123!"
                    Role = "Admin" // <--- This gives User 1 super powers
                },
                new User
                {
                    Id = 2,
                    Name = "Demo Seller",
                    Email = "seller@zoomies.com",
                    PasswordHash = "$2a$11$gqfJFPQzPs3HykWpu3P6z.fpG74WvoI5SBpYjKLyMvRxH57ESHQtq",
                    Role = "User"
                }
            );

            modelBuilder.Entity<ContactMessage>()
                .HasOne(m => m.Car)
                .WithMany()
                .HasForeignKey(m => m.CarId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishlistItem>()
                .HasIndex(w => new { w.UserId, w.CarId })
                .IsUnique();

            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Car)
                .WithMany()
                .HasForeignKey(w => w.CarId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // This property represents the table in your database
        public DbSet<Car> Cars { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<WishlistItem> WishlistItems { get; set; }
    }
}
