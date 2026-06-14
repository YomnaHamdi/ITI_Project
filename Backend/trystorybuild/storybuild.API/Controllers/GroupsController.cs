using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/groups")]
public class GroupsController(
    IStudentGroupRepository groupRepository,
    ILessonAssignmentRepository assignmentRepository,
    IStudentRepository studentRepository) : ControllerBase
{
    // ── Get teacher's groups ───────────────────────────────────────────────────
    [HttpGet("teacher/{teacherId:guid}")]
    public async Task<IActionResult> GetByTeacher(Guid teacherId)
    {
        var groups = await groupRepository.GetByTeacherIdAsync(teacherId);
        var dtos = groups.Select(g => new StudentGroupDto(
            g.Id, g.Name, g.TeacherId, g.Members.Count, g.CreatedAt,
            g.Members.Select(m => new StudentGroupMemberDto(
                m.StudentId, m.Student.Name, m.AddedAt)).ToList()
        )).ToList();
        return Ok(dtos);
    }

    // ── Create group ───────────────────────────────────────────────────────────
    [HttpPost("teacher/{teacherId:guid}")]
    public async Task<IActionResult> Create(Guid teacherId, [FromBody] CreateGroupRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "يرجى إدخال اسم المجموعة." });

        var group = new StudentGroup { Name = req.Name.Trim(), TeacherId = teacherId };
        var saved = await groupRepository.SaveAsync(group);
        return Ok(new StudentGroupDto(saved.Id, saved.Name, saved.TeacherId, 0, saved.CreatedAt, new()));
    }

    // ── Add member ─────────────────────────────────────────────────────────────
    [HttpPost("{groupId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid groupId, [FromBody] AddGroupMemberRequest req)
    {
        var student = await studentRepository.FindByIdAsync(req.StudentId);
        if (student is null)
            return NotFound(new { error = "الطالب غير موجود." });

        var added = await groupRepository.AddMemberAsync(groupId, req.StudentId);
        return added ? Ok(new { message = "تمت الإضافة." }) : Conflict(new { error = "الطالب موجود بالفعل في المجموعة." });
    }

    // ── Remove member ──────────────────────────────────────────────────────────
    [HttpDelete("{groupId:guid}/members/{studentId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid groupId, Guid studentId)
    {
        var removed = await groupRepository.RemoveMemberAsync(groupId, studentId);
        return removed ? Ok(new { message = "تمت الإزالة." }) : NotFound(new { error = "العضو غير موجود." });
    }

    // ── Delete group ───────────────────────────────────────────────────────────
    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid groupId)
    {
        var deleted = await groupRepository.DeleteAsync(groupId);
        return deleted ? NoContent() : NotFound(new { error = "المجموعة غير موجودة." });
    }

    // ── Assign lesson to student or group ──────────────────────────────────────
    [HttpPost("assign")]
    public async Task<IActionResult> AssignLesson([FromBody] AssignLessonRequest req)
    {
        if (req.TargetStudentId is null && req.TargetGroupId is null)
            return BadRequest(new { error = "يرجى تحديد طالب أو مجموعة." });

        var assignment = new LessonAssignment
        {
            LessonId        = req.LessonId,
            TeacherId       = Guid.Empty, // caller should pass teacherId
            TargetType      = req.TargetType,
            TargetStudentId = req.TargetStudentId,
            TargetGroupId   = req.TargetGroupId
        };
        var saved = await assignmentRepository.SaveAsync(assignment);
        return Ok(new { id = saved.Id, message = "تم التعيين بنجاح." });
    }

    // ── Get assignments for teacher ────────────────────────────────────────────
    [HttpGet("assignments/teacher/{teacherId:guid}")]
    public async Task<IActionResult> GetTeacherAssignments(Guid teacherId)
    {
        var assignments = await assignmentRepository.GetByTeacherAsync(teacherId);
        var dtos = assignments.Select(a => new LessonAssignmentDto(
            a.Id, a.LessonId, a.Lesson?.Title ?? "",
            a.TargetType,
            a.TargetStudentId, null,
            a.TargetGroupId, null,
            a.AssignedAt)).ToList();
        return Ok(dtos);
    }

    // ── Get assigned lessons for student ──────────────────────────────────────
    [HttpGet("assigned/student/{studentId:guid}")]
    public async Task<IActionResult> GetStudentAssigned(Guid studentId)
    {
        var groups   = await groupRepository.GetGroupsForStudentAsync(studentId);
        var groupIds = groups.Select(g => g.Id).ToList();
        var assignments = await assignmentRepository.GetForStudentAsync(studentId, groupIds);

        var dtos = assignments.Select(a => new LessonAssignmentDto(
            a.Id, a.LessonId, a.Lesson?.Title ?? "",
            a.TargetType,
            a.TargetStudentId, null,
            a.TargetGroupId, null,
            a.AssignedAt)).ToList();
        return Ok(dtos);
    }
}
