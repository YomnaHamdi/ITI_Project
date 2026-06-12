using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.SystemAdmin))
        {
            var admin = new User
            {
                Name         = "System Admin",
                Email        = "admin@lughati.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@Lughati2026"),
                Role         = UserRole.SystemAdmin,
                IsActive     = true,
                IsBlocked    = false
            };
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}
