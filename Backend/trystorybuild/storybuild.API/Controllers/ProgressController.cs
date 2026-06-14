using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/progress")]
    public class ProgressController(IStudentProgressRepository progressRepository) : ControllerBase
    {
        /// <summary>Get student progress for a specific story.</summary>
        [HttpGet("{storyId:guid}/{childName}")]
        [ProducesResponseType(typeof(ProgressResponse), 200)]
        public async Task<IActionResult> Get(Guid storyId, string childName)
        {
            var progress = await progressRepository.GetAsync(storyId, childName);
            if (progress is null)
                return Ok(new ProgressResponse(storyId, childName, 1, 0, 0, 0, false));

            return Ok(new ProgressResponse(
                progress.StoryId ?? storyId,
                progress.ChildName,
                progress.CurrentPage,
                progress.TotalQuestions,
                progress.CorrectAnswers,
                progress.ScorePercentage,
                progress.ExamCompleted));
        }

        /// <summary>Update student page progress.</summary>
        [HttpPut]
        [ProducesResponseType(typeof(ProgressResponse), 200)]
        public async Task<IActionResult> Update([FromBody] ProgressResponse request)
        {
            var progress = new StudentProgress
            {
                StoryId = request.StoryId,
                ChildName = request.ChildName,
                CurrentPage = request.CurrentPage,
                TotalQuestions = request.TotalQuestions,
                CorrectAnswers = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            return Ok(request);
        }

        /// <summary>Update lesson exam progress after student submits lesson exam.</summary>
        [HttpPut("lesson")]
        [ProducesResponseType(typeof(LessonProgressRequest), 200)]
        public async Task<IActionResult> UpdateLesson([FromBody] LessonProgressRequest request)
        {
            var progress = new StudentProgress
            {
                LessonId       = request.LessonId,
                ChildName      = request.ChildName,
                TotalQuestions = request.TotalQuestions,
                CorrectAnswers = request.CorrectAnswers,
                ScorePercentage = request.ScorePercentage,
                ExamCompleted  = request.ExamCompleted
            };

            await progressRepository.SaveAsync(progress);
            return Ok(request);
        }
    }

}
