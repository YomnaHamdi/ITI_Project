using Application.Agent;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/story")]
    public class StoryController(
    StoryAgent storyAgent,
    IStoryRepository storyRepository) : ControllerBase
    {
        // POST /api/story/generate
        // Frontend sends: { childName, character, theme }
        // Agent: Qwen2.5 → story JSON → ComfyUI × 3 → SQL Server → response
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateStoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Generate([FromBody] GenerateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ChildName) ||
                string.IsNullOrWhiteSpace(request.Character) ||
                string.IsNullOrWhiteSpace(request.Theme))
            {
                return BadRequest(new { error = "يرجى إرسال اسم الطفل والشخصية والموضوع." });
            }

            var response = await storyAgent.RunAsync(request);
            return Ok(response);
        }

        // GET /api/story/{id}
        // Reload a previously generated story (used after page refresh)
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GenerateStoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var story = await storyRepository.GetByIdAsync(id);
            if (story is null)
                return NotFound(new { error = "القصة غير موجودة." });

            var response = new GenerateStoryResponse(
                story.Id,
                story.Title,
                story.Pages
                     .OrderBy(p => p.PageNumber)
                     .Select(p => new StoryPageResponse(p.PageNumber, p.Sentence, p.ImagePath))
                     .ToList());

            return Ok(response);
        }
    }

}
