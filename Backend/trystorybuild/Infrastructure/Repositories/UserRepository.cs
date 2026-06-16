using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class UserRepository(AppDbContext db) : IUserRepository
    {
        public Task<User?> FindByEmailAsync(string email) =>
            db.Users.FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> FindByIdAsync(Guid id) =>
            db.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User> SaveAsync(User user)
        {
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<Parent> SaveParentAsync(Parent parent)
        {
            db.Parents.Add(parent);
            await db.SaveChangesAsync();
            return parent;
        }

        public async Task<Teacher> SaveTeacherAsync(Teacher teacher)
        {
            db.Teachers.Add(teacher);
            await db.SaveChangesAsync();
            return teacher;
        }

        public Task<Teacher?> GetTeacherByIdAsync(Guid userId) =>
            db.Teachers.FirstOrDefaultAsync(t => t.Id == userId);

        public Task<List<Teacher>> GetTeachersBySchoolCodeAsync(string schoolCode) =>
            db.Teachers
              .Include(t => t.User)
              .Where(t => t.SchoolCode == schoolCode)
              .ToListAsync();
    }
}
