using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class StudentRepository(AppDbContext db) : IStudentRepository
    {
        public Task<Student?> FindByUsernameAsync(string username) =>
            db.Students.FirstOrDefaultAsync(s => s.Username == username);

        public Task<Student?> FindByIdAsync(Guid id) =>
            db.Students.FirstOrDefaultAsync(s => s.Id == id);

        public async Task<Student> SaveAsync(Student student)
        {
            db.Students.Add(student);
            await db.SaveChangesAsync();
            return student;
        }

        public Task<List<Student>> GetByParentIdAsync(Guid parentId) =>
            db.Students.Where(s => s.ParentId == parentId).ToListAsync();

        public Task<List<Student>> GetByTeacherIdAsync(Guid teacherId) =>
            db.Students.Where(s => s.TeacherId == teacherId).ToListAsync();
    }
}
