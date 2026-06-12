using Application.Agent;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/exam")]
    public class ExamController(
        ExamAgent examAgent,
        IExamRepository examRepository) : ControllerBase
    {
        [HttpPost("generate/{storyId:guid}")]
        [ProducesResponseType(typeof(ExamResponse), 200)]
        public async Task<IActionResult> Generate(Guid storyId)
        {
            var existing = await examRepository.GetByStoryIdAsync(storyId);
            if (existing is not null)
                return Ok(ExamAgent.MapToResponse(existing, storyId));

            var result = await examAgent.GenerateAsync(storyId);
            return Ok(result);
        }

        [HttpPost("generate/lesson/{lessonId:guid}")]
        [ProducesResponseType(typeof(ExamResponse), 200)]
        public async Task<IActionResult> GenerateLesson(Guid lessonId)
        {
            var existing = await examRepository.GetByLessonIdAsync(lessonId);
            if (existing is not null)
                return Ok(ExamAgent.MapToResponse(existing, lessonId));

            var result = await examAgent.GenerateFromLessonAsync(lessonId);
            return Ok(result);
        }

        [HttpGet("story/{storyId:guid}")]
        [ProducesResponseType(typeof(ExamResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByStory(Guid storyId)
        {
            var exam = await examRepository.GetByStoryIdAsync(storyId);
            if (exam is null)
                return NotFound(new { error = "لا يوجد امتحان لهذه القصة بعد." });

            return Ok(ExamAgent.MapToResponse(exam, storyId));
        }

        [HttpPost("submit")]
        [ProducesResponseType(typeof(ExamResultResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Submit([FromBody] SubmitExamRequest request)
        {
            if (request.Answers is null || request.Answers.Count == 0)
                return BadRequest(new { error = "يرجى إرسال الإجابات." });

            var result = await examAgent.SubmitAsync(request);
            return Ok(result);
        }
    }
}
