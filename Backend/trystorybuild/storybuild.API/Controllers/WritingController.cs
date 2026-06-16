using Application.Agent;
using Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/writing")]
    public class WritingController(WritingCorrectionAgent writingAgent) : ControllerBase
    {
        // ── Lesson-based evaluation (existing flow) ───────────────────────────────
        [HttpPost("evaluate")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(WritingCorrectionResponse), 200)]
        public async Task<IActionResult> Evaluate(
            [FromForm] Guid lessonId,
            [FromForm] Guid lessonPageId,
            [FromForm] string childName,
            IFormFile image)
        {
             Console.WriteLine($"Received: lessonId={lessonId}, lessonPageId={lessonPageId}, childName={childName}, image={image?.FileName}");
    
            if (image is null || image.Length == 0)
                return BadRequest(new { error = "يرجى رفع صورة الكتابة." });

            if (string.IsNullOrWhiteSpace(childName))
                return BadRequest(new { error = "يرجى إرسال اسم الطفل." });

            var allowed = new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };
            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = "يرجى رفع صورة بصيغة PNG أو JPG." });

            if (image.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "حجم الصورة كبير جداً (الحد الأقصى 10 ميغابايت)." });

            var result = await writingAgent.EvaluateAsync(lessonPageId, lessonId, childName, image);
            return Ok(result);
        }

        // ── Standalone canvas evaluation (new) ────────────────────────────────────
        [HttpPost("canvas")]
        [ProducesResponseType(typeof(WritingCorrectionResponse), 200)]
        public async Task<IActionResult> EvaluateCanvas([FromBody] CanvasEvaluationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
                return BadRequest(new { error = "الرجاء توفير صورة الكتابة (base64)." });

            if (string.IsNullOrWhiteSpace(request.ExpectedText))
                return BadRequest(new { error = "الرجاء توفير الجملة المطلوبة." });

            var result = await writingAgent.EvaluateDirectAsync(request.ImageBase64, request.ExpectedText);
            return Ok(result);
        }
    }

    public record CanvasEvaluationRequest(string ImageBase64, string ExpectedText);
}
