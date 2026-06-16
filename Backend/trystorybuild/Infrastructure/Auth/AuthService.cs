using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Auth
{
    public class AuthService(
        IUserRepository    userRepo,
        IStudentRepository studentRepo,
        IConfiguration     config) : IAuthService
    {
        // ── Adult registration ──────────────────────────────────────────────────
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await userRepo.FindByEmailAsync(request.Email) is not null)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل.");

            var role = request.Role.ToLower() switch
            {
                "parent"  => UserRole.Parent,
                "teacher" => UserRole.Teacher,
                _         => throw new ArgumentException("دور غير صالح.")
            };

            var user = new User
            {
                Name         = request.FullName,
                Email        = request.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role         = role,
            };
            await userRepo.SaveAsync(user);

            if (role == UserRole.Parent)
                await userRepo.SaveParentAsync(new Parent { Id = user.Id });
            else
                await userRepo.SaveTeacherAsync(new Teacher
                {
                    Id         = user.Id,
                    IsPrivate  = string.IsNullOrWhiteSpace(request.SchoolCode),
                    SchoolCode = request.SchoolCode
                });

            return ToAuthResponse(user);
        }

        // ── Adult login ─────────────────────────────────────────────────────────
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await userRepo.FindByEmailAsync(request.Email.Trim().ToLower())
                ?? throw new InvalidOperationException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new InvalidOperationException("البريد الإلكتروني أو كلمة المرور غير صحيحة.");

            if (!user.IsActive || user.IsBlocked)
                throw new InvalidOperationException("الحساب موقوف. تواصل مع الدعم.");

            return ToAuthResponse(user);
        }

        // ── Create student (by parent/teacher) ──────────────────────────────────
        public async Task<StudentAuthResponse> CreateStudentAsync(Guid creatorId, CreateStudentRequest request)
        {
            var normalised = request.Username.Trim().ToLower();
            if (await studentRepo.FindByUsernameAsync(normalised) is not null)
                throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل.");

            if (request.ImagePin1 < 1 || request.ImagePin1 > 20)
                throw new ArgumentException("رمز الصورة يجب أن يكون بين 1 و 20.");

            var creator = await userRepo.FindByIdAsync(creatorId)
                ?? throw new InvalidOperationException("المستخدم غير موجود.");

            var student = new Student
            {
                Name        = request.Name,
                Age         = request.Age,
                Username    = normalised,
                ImagePin1   = request.ImagePin1,
                ImagePin2   = request.ImagePin2,
                Level       = request.Level,
                LoginMethod = StudentLoginMethod.ImagePin,
                ParentId    = creator.Role == UserRole.Parent  ? creatorId : null,
                TeacherId   = creator.Role == UserRole.Teacher ? creatorId : null,
            };
            await studentRepo.SaveAsync(student);

            return ToStudentResponse(student);
        }

        // ── Student login ───────────────────────────────────────────────────────
        public async Task<StudentAuthResponse> StudentLoginAsync(StudentLoginRequest request)
        {
            var student = await studentRepo.FindByUsernameAsync(request.Username.Trim().ToLower())
                ?? throw new InvalidOperationException("اسم المستخدم غير موجود.");

            if (student.ImagePin1 != request.ImagePin1 || student.ImagePin2 != request.ImagePin2)
                throw new InvalidOperationException("رمز الصورة غير صحيح.");

            return ToStudentResponse(student);
        }

        // ── List children (for parent dashboard) ───────────────────────────────
        public async Task<List<StudentProfileDto>> GetChildrenAsync(Guid parentId)
        {
            var children = await studentRepo.GetByParentIdAsync(parentId);
            return children.Select(ToSummary).ToList();
        }

        // ── List students (for teacher dashboard) ───────────────────────────────
        public async Task<List<StudentProfileDto>> GetStudentsAsync(Guid teacherId)
        {
            var students = await studentRepo.GetByTeacherIdAsync(teacherId);
            return students.Select(ToSummary).ToList();
        }

        // ── Update student level after placement test ───────────────────────────
        public async Task<StudentAuthResponse> UpdateStudentLevelAsync(Guid studentId, int level)
        {
            var ok = await studentRepo.UpdateLevelAsync(studentId, level);
            if (!ok) throw new InvalidOperationException("الطالب غير موجود.");
            var student = await studentRepo.FindByIdAsync(studentId)
                ?? throw new InvalidOperationException("الطالب غير موجود.");
            return ToStudentResponse(student);
        }

        // ── Create school admin (system admin only) ─────────────────────────────
        public async Task<(Guid id, string schoolCode)> CreateSchoolAdminAsync(
            string schoolName, string email, string password)
        {
            var normalised = email.Trim().ToLower();
            if (await userRepo.FindByEmailAsync(normalised) is not null)
                throw new InvalidOperationException("البريد الإلكتروني مستخدم بالفعل.");

            var user = new User
            {
                Name         = schoolName.Trim(),
                Email        = normalised,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role         = UserRole.SchoolAdmin,
            };
            await userRepo.SaveAsync(user);

            var schoolCode = user.Id.ToString("N")[..8].ToUpper();
            return (user.Id, schoolCode);
        }

        // ── Token generation ────────────────────────────────────────────────────
        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token  = new JwtSecurityToken(
                issuer:             config["Jwt:Issuer"],
                audience:           config["Jwt:Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddDays(30),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private AuthResponse ToAuthResponse(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name,             user.Name),
                new Claim(ClaimTypes.Email,            user.Email),
                new Claim(ClaimTypes.Role,             user.Role.ToString()),
            };
            var expiry = DateTime.UtcNow.AddDays(30);
            return new AuthResponse(
                GenerateToken(claims),
                user.Id.ToString(),
                user.Name,
                user.Role.ToString().ToLower(),
                expiry);
        }

        private StudentAuthResponse ToStudentResponse(Student student)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, student.Id.ToString()),
                new Claim(ClaimTypes.Name,             student.Name),
                new Claim(ClaimTypes.Role,             "student"),
                new Claim("level",                     student.Level.ToString()),
            };
            var expiry = DateTime.UtcNow.AddDays(30);
            return new StudentAuthResponse(
                GenerateToken(claims),
                student.Id.ToString(),
                student.Name,
                student.Level,
                student.PlacementDone,
                expiry);
        }

        private static StudentProfileDto ToSummary(Student s) =>
            new(s.Id, s.Name, s.Age, s.Username, s.Level, s.PlacementDone, s.AvatarUrl);
    }
}
