using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/placement")]
    public class PlacementController(IPlacementRepository repository) : ControllerBase
    {
        [HttpGet("questions")]
        [ProducesResponseType(typeof(List<PlacementQuestionDto>), 200)]
        public async Task<IActionResult> GetQuestions()
        {
            await repository.SeedAsync();
            var questions = await repository.GetAllAsync();
            return Ok(questions.Select(MapToDto).ToList());
        }

        [HttpPost("submit")]
        [ProducesResponseType(typeof(PlacementResultDto), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Submit([FromBody] PlacementSubmitRequest request)
        {
            if (request.Answers == null || request.Answers.Count == 0)
                return BadRequest(new { error = "يرجى إرسال الإجابات." });

            var allQuestions = await repository.GetAllAsync();
            var answerMap    = request.Answers.ToDictionary(a => a.QuestionId, a => a.Answer);

            int p1    = ScorePart(allQuestions, answerMap, 1);
            int p2    = ScorePart(allQuestions, answerMap, 2);
            int p3    = ScorePart(allQuestions, answerMap, 3);
            int total = p1 + p2 + p3;

            // Strict rule: must score 5/5 to advance; fail S1 or S2 → Level 1; fail S3 → Level 2
            int level = p1 < 5 ? 1 : (p2 < 5 ? 1 : (p3 < 5 ? 2 : 3));

            string[] levelNames = ["", "الحروف والأصوات", "الكلمات والمفردات", "الجمل والقصص"];
            string[] messages   =
            [
                "",
                "أنت في المستوى الأول! ستتعلم الحروف العربية والأصوات.",
                "رائع! أنت في المستوى الثاني! ستتعلم الكلمات والمفردات.",
                "ممتاز! أنت في المستوى الثالث! ستقرأ الجمل والقصص."
            ];

            return Ok(new PlacementResultDto(total, p1, p2, p3, level, levelNames[level], messages[level]));
        }

        private static int ScorePart(
            List<PlacementQuestion> questions,
            Dictionary<Guid, string> answerMap,
            int part) =>
            questions
                .Where(q => q.Part == part)
                .Count(q => answerMap.TryGetValue(q.Id, out var ans) && ans == q.CorrectAnswer);

        private static readonly JsonSerializerOptions JsonOpts =
            new() { PropertyNameCaseInsensitive = true };

        private static PlacementQuestionDto MapToDto(PlacementQuestion q)
        {
            var options = JsonSerializer.Deserialize<List<OptionRaw>>(q.OptionsJson, JsonOpts) ?? [];
            return new PlacementQuestionDto(
                q.Id, q.Part, q.Order,
                q.QuestionText, q.ImageContent,
                options.Select(o => new PlacementOptionDto(o.Key, o.Emoji, o.Label)).ToList(),
                q.AudioText);
        }

        private sealed class OptionRaw
        {
            public string Key   { get; set; } = string.Empty;
            public string Emoji { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
        }
    }
}
