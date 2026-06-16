using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/classrooms")]
[Authorize]
public class ClassroomsController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("Invalid token."));

    private string SchoolCode() => CurrentUserId().ToString("N")[..8].ToUpper();

    // ── GET /api/classrooms  (SchoolAdmin) ────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> GetMyClassrooms()
    {
        var code = SchoolCode();
        var classrooms = await db.Classrooms
            .Where(c => c.SchoolCode == code)
            .Include(c => c.Students)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var teacherIds = classrooms.Select(c => c.TeacherId).Distinct().ToList();
        var teachers   = await db.Users.Where(u => teacherIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name);

        var result = classrooms.Select(c => new
        {
            id           = c.Id,
            name         = c.Name,
            level        = c.Level,
            teacherId    = c.TeacherId,
            teacherName  = teachers.GetValueOrDefault(c.TeacherId, ""),
            studentCount = c.Students.Count,
            avgProgress  = 0
        });
        return Ok(result);
    }

    // ── GET /api/classrooms/{id}  (detail + students) ─────────────────────────
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SchoolAdmin,Teacher")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var classroom = await db.Classrooms
            .Include(c => c.Students).ThenInclude(cs => cs.Student)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (classroom is null) return NotFound();

        var teacher = await db.Users.FindAsync(classroom.TeacherId);

        return Ok(new
        {
            id          = classroom.Id,
            name        = classroom.Name,
            level       = classroom.Level,
            teacherId   = classroom.TeacherId,
            teacherName = teacher?.Name ?? "",
            students    = classroom.Students.Select(cs => new
            {
                id            = cs.Student.Id,
                name          = cs.Student.Name,
                username      = cs.Student.Username,
                level         = cs.Student.Level,
                placementDone = cs.Student.PlacementDone,
            })
        });
    }

    // ── POST /api/classrooms  (create) ────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateClassroomRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || req.TeacherId == Guid.Empty)
            return BadRequest("Name and TeacherId are required.");

        var classroom = new Classroom
        {
            Name       = req.Name.Trim(),
            Level      = req.Level,
            SchoolCode = SchoolCode(),
            TeacherId  = req.TeacherId,
        };
        db.Classrooms.Add(classroom);
        await db.SaveChangesAsync();

        var teacher = await db.Users.FindAsync(classroom.TeacherId);
        return Ok(new
        {
            id           = classroom.Id,
            name         = classroom.Name,
            level        = classroom.Level,
            teacherId    = classroom.TeacherId,
            teacherName  = teacher?.Name ?? "",
            studentCount = 0,
            avgProgress  = 0
        });
    }

    // ── PUT /api/classrooms/{id}  (edit name / level / teacher) ──────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> Edit(Guid id, [FromBody] EditClassroomRequest req)
    {
        var classroom = await db.Classrooms.FindAsync(id);
        if (classroom is null) return NotFound();
        if (classroom.SchoolCode != SchoolCode()) return Forbid();

        if (!string.IsNullOrWhiteSpace(req.Name)) classroom.Name = req.Name.Trim();
        if (req.Level is > 0)                      classroom.Level = req.Level!.Value;
        if (req.TeacherId.HasValue && req.TeacherId.Value != Guid.Empty)
            classroom.TeacherId = req.TeacherId.Value;

        await db.SaveChangesAsync();

        var teacher = await db.Users.FindAsync(classroom.TeacherId);
        return Ok(new { id = classroom.Id, name = classroom.Name, level = classroom.Level,
                         teacherId = classroom.TeacherId, teacherName = teacher?.Name ?? "" });
    }

    // ── DELETE /api/classrooms/{id}  ─────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var classroom = await db.Classrooms.FindAsync(id);
        if (classroom is null) return NotFound();
        if (classroom.SchoolCode != SchoolCode()) return Forbid();
        db.Classrooms.Remove(classroom);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── POST /api/classrooms/{id}/students  (add student) ────────────────────
    [HttpPost("{id:guid}/students")]
    [Authorize(Roles = "SchoolAdmin,Teacher")]
    public async Task<IActionResult> AddStudent(Guid id, [FromBody] AddClassroomStudentRequest req)
    {
        var classroom = await db.Classrooms.FindAsync(id);
        if (classroom is null) return NotFound();

        var student = await db.Students.FindAsync(req.StudentId);
        if (student is null) return NotFound("Student not found.");

        bool exists = await db.ClassroomStudents
            .AnyAsync(cs => cs.ClassroomId == id && cs.StudentId == req.StudentId);
        if (exists) return Ok(new { message = "Already enrolled." });

        student.TeacherId = classroom.TeacherId;
        db.ClassroomStudents.Add(new ClassroomStudent { ClassroomId = id, StudentId = req.StudentId });
        await db.SaveChangesAsync();
        return Ok(new { message = "Student added.", studentId = student.Id, name = student.Name });
    }

    // ── DELETE /api/classrooms/{id}/students/{studentId} ─────────────────────
    [HttpDelete("{id:guid}/students/{studentId:guid}")]
    [Authorize(Roles = "SchoolAdmin,Teacher")]
    public async Task<IActionResult> RemoveStudent(Guid id, Guid studentId)
    {
        var cs = await db.ClassroomStudents
            .FirstOrDefaultAsync(x => x.ClassroomId == id && x.StudentId == studentId);
        if (cs is null) return NotFound();
        db.ClassroomStudents.Remove(cs);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── GET /api/classrooms/school-students?q=  (search for student to add) ──
    [HttpGet("school-students")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> SearchSchoolStudents([FromQuery] string q = "")
    {
        var code = SchoolCode();
        // Find teachers belonging to this school
        var teacherIds = await db.Teachers
            .Where(t => t.SchoolCode == code)
            .Select(t => t.Id).ToListAsync();

        var query = db.Students.Where(s =>
            (s.TeacherId.HasValue && teacherIds.Contains(s.TeacherId.Value))
            || !s.TeacherId.HasValue);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(s => s.Name.Contains(q) || s.Username.Contains(q));

        var students = await query.Take(20).Select(s => new
        {
            id       = s.Id,
            name     = s.Name,
            username = s.Username,
            level    = s.Level,
        }).ToListAsync();

        return Ok(students);
    }

    // ── GET /api/classrooms/report  (per-classroom detailed report) ───────────
    [HttpGet("report")]
    [Authorize(Roles = "SchoolAdmin")]
    public async Task<IActionResult> GetReport()
    {
        var code = SchoolCode();
        var classrooms = await db.Classrooms
            .Where(c => c.SchoolCode == code)
            .Include(c => c.Students).ThenInclude(cs => cs.Student)
            .OrderBy(c => c.Level).ThenBy(c => c.Name)
            .ToListAsync();

        var teacherIds  = classrooms.Select(c => c.TeacherId).Distinct().ToList();
        var teachers    = await db.Users.Where(u => teacherIds.Contains(u.Id))
                                        .ToDictionaryAsync(u => u.Id, u => u.Name);

        var allProgress = await db.StudentProgress.Where(p => p.ExamCompleted).ToListAsync();

        var result = classrooms.Select(c =>
        {
            var studentNames = c.Students.Select(cs => cs.Student.Name).ToList();
            var prog = allProgress.Where(p => studentNames.Contains(p.ChildName)).ToList();
            double avg = prog.Any() ? Math.Round(prog.Average(p => p.ScorePercentage), 1) : 0;

            return new
            {
                classroomId  = c.Id,
                classroomName= c.Name,
                level        = c.Level,
                teacherName  = teachers.GetValueOrDefault(c.TeacherId, "غير محدد"),
                studentCount = c.Students.Count,
                avgScore     = avg,
                students     = c.Students.Select(cs => new
                {
                    name       = cs.Student.Name,
                    username   = cs.Student.Username,
                    level      = cs.Student.Level,
                    placementDone = cs.Student.PlacementDone,
                    avgScore   = Math.Round(
                        allProgress.Where(p => p.ChildName == cs.Student.Name).DefaultIfEmpty()
                                   .Average(p => p?.ScorePercentage ?? 0), 1)
                }).ToList()
            };
        }).ToList();

        return Ok(result);
    }

    // ── GET /api/classrooms/my  (Teacher — own classrooms + students) ──────────
    [HttpGet("my")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetMyTeacherClassrooms()
    {
        var teacherId = CurrentUserId();
        var classrooms = await db.Classrooms
            .Where(c => c.TeacherId == teacherId)
            .Include(c => c.Students).ThenInclude(cs => cs.Student)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = classrooms.Select(c => new
        {
            id       = c.Id,
            name     = c.Name,
            level    = c.Level,
            students = c.Students.Select(cs => new
            {
                id            = cs.Student.Id,
                name          = cs.Student.Name,
                username      = cs.Student.Username,
                level         = cs.Student.Level,
                placementDone = cs.Student.PlacementDone,
            })
        });
        return Ok(result);
    }
}

public record CreateClassroomRequest(string Name, int Level, Guid TeacherId);
public record EditClassroomRequest(string? Name, int? Level, Guid? TeacherId);
public record AddClassroomStudentRequest(Guid StudentId);
