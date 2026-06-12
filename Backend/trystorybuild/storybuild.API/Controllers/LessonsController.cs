using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonsController(ILessonRepository lessonRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<LessonSummaryDto>), 200)]
    public async Task<IActionResult> GetByLevel([FromQuery] int level = 1)
    {
        if (level < 1)
            return BadRequest(new { error = "المستوى غير صالح." });

        var lessons = await lessonRepository.GetByLevelAsync(level);
        return Ok(lessons.Select(LessonMapper.ToSummary).ToList());
    }

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
}