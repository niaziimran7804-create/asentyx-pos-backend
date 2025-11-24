using Microsoft.EntityFrameworkCore;
using POS.Api.Models;

namespace POS.Api.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Apply pending migrations
            await context.Database.MigrateAsync();

            // Check if users already exist
            if (context.Users.Any())
            {
                return; // Database has been seeded
            }

            // Create initial admin user
            var adminUser = new User
            {
                UserId = "admin",
                FirstName = "Admin",
                LastName = "User",
                Password = "admin123", // Plain text for now - matches current AuthService implementation
                Email = "admin@pos.com",
                Age = 30,
                Gender = "Male",
                Role = "Admin",
                Salary = 50000,
                JoinDate = DateTime.UtcNow,
                Birthdate = new DateTime(1994, 1, 1),
                Phone = "1234567890",
                CurrentCity = "Default City",
                Division = "Default Division"
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}

