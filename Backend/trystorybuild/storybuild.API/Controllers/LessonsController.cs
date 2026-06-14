using Application.Agent;
using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonsController(
    ILessonRepository lessonRepository,
    LessonGenerationAgent generationAgent) : ControllerBase
{
    // ── Student: get lessons by level ──────────────────────────────────────────
    [HttpGet]
    [ProducesResponseType(typeof(List<LessonSummaryDto>), 200)]
    public async Task<IActionResult> GetByLevel([FromQuery] int level = 1)
    {
        if (level < 1)
            return BadRequest(new { error = "المستوى غير صالح." });

        var lessons = await lessonRepository.GetByLevelAsync(level);
        return Ok(lessons.Select(LessonMapper.ToSummary).ToList());
    }

    // ── Get lesson detail ──────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LessonDetailResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var lesson = await lessonRepository.GetByIdAsync(id);
        if (lesson is null)
            return NotFound(new { error = "الدرس غير موجود." });

        return Ok(LessonMapper.ToDetail(lesson));
    }

    // ── Generate lesson (Teacher / Student prompt) ─────────────────────────────
    [HttpPost("generate")]
    [ProducesResponseType(typeof(LessonDetailResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateLessonRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest(new { error = "يرجى إدخال موضوع الدرس." });
        if (request.Level is < 1 or > 4)
            return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 4." });

        try
        {
            var lesson = await generationAgent.GenerateAsync(request, ct);
            return Ok(lesson);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "فشل توليد الدرس.", detail = ex.Message });
        }
    }

    // ── Create manual lesson (teacher block builder) ───────────────────────────
    [HttpPost("manual")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateManual([FromBody] CreateManualLessonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "يرجى إدخال عنوان الدرس." });
        if (request.Pages == null || request.Pages.Count == 0)
            return BadRequest(new { error = "يرجى إضافة صفحة واحدة على الأقل." });

        var lesson = new Domain.Entities.Lesson
        {
            Title       = request.Title.Trim(),
            Level       = request.Level,
            Letter      = request.Letter.Trim(),
            LetterName  = request.Letter.Trim(),
            CreatorId   = request.CreatorId,
            CreatorRole = "Teacher",
            IsGenerated = false,
        };

        int pageNum = 1;
        foreach (var p in request.Pages)
        {
            lesson.Pages.Add(new Domain.Entities.LessonPage
            {
                PageNumber  = pageNum++,
                Sentence    = p.Type == "text"  ? p.Content.Trim() : string.Empty,
                ImagePath   = p.Type == "image" ? p.Content.Trim() : string.Empty,
                ImagePrompt = p.Type == "image" ? p.Content.Trim() : string.Empty,
                IsUnlocked  = true,
            });
        }

        var created = await lessonRepository.CreateManualAsync(lesson);
        return Ok(Application.Mapping.LessonMapper.ToDetail(created));
    }

    // ── Delete lesson ──────────────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await lessonRepository.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "الدرس غير موجود." });
        return NoContent();
    }

    // ── Get lessons created by a specific user (my lessons) ───────────────────
    [HttpGet("my/{creatorId:guid}")]
    [ProducesResponseType(typeof(List<LessonSummaryDto>), 200)]
    public async Task<IActionResult> GetMyLessons(Guid creatorId)
    {
        var all = await lessonRepository.GetAllAsync();
        var mine = all
            .Where(l => l.CreatorId == creatorId && l.IsGenerated)
            .Select(LessonMapper.ToSummary)
            .ToList();
        return Ok(mine);
    }
}
