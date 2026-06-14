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
        /// <summary>Generate a new 3-page Arabic story with images.</summary>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateStoryResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Generate([FromBody] GenerateStoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ChildName) ||
                string.IsNullOrWhiteSpace(request.Character) ||
                string.IsNullOrWhiteSpace(request.Theme))
                return BadRequest(new { error = "يرجى إرسال اسم الطفل والشخصية والموضوع." });

            var result = await storyAgent.RunAsync(request);
            return Ok(result);
        }

        /// <summary>Load a previously generated story by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GenerateStoryResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var story = await storyRepository.GetByIdAsync(id);
            if (story is null) return NotFound(new { error = "القصة غير موجودة." });
            return Ok(StoryAgent.MapToResponse(story));
        }

        /// <summary>List all stories.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<GenerateStoryResponse>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var stories = await storyRepository.GetAllAsync();
            return Ok(stories.Select(StoryAgent.MapToResponse).ToList());
        }

        /// <summary>List stories belonging to a specific child.</summary>
        [HttpGet("mine/{childName}")]
        [ProducesResponseType(typeof(List<GenerateStoryResponse>), 200)]
        public async Task<IActionResult> GetMine(string childName)
        {
            var stories = await storyRepository.GetByChildNameAsync(childName);
            return Ok(stories.Select(StoryAgent.MapToResponse).ToList());
        }

        /// <summary>Delete a story by ID.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await storyRepository.DeleteAsync(id);
            if (!deleted) return NotFound(new { error = "القصة غير موجودة." });
            return NoContent();
        }
    }

}
