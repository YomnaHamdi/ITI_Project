using Application.Agent;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/rag")]
    public class RagController(
        IRagIngestionService ingestionService,
        IRagQueryService queryService,
        IRagPageChunkRepository pageChunkRepository,
        IEducationalPdfIngestionService educationalIngestionService,
        AppDbContext db) : ControllerBase
    {
        [HttpPost("ingest")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(IngestDocumentResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Ingest(
            IFormFile file,
            [FromForm] string? letter,
            [FromForm] int? level,
            [FromForm] string? tags,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "يرجى رفع ملف." });

            var allowed = new[] { ".pdf", ".pptx", ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = $"نوع الملف '{ext}' غير مدعوم." });

            try
            {
                var request = new IngestDocumentRequest(letter?.Trim(), level, tags?.Trim());
                var result  = await ingestionService.IngestAsync(
                    file.OpenReadStream(), file.FileName, request, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Ingest educational PDF (per-page, Gemini Vision) ──────────────────────
        [HttpPost("ingest-educational")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(IngestDocumentResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> IngestEducational(
            IFormFile file,
            [FromForm] int level,
            [FromForm] string letter,
            [FromForm] string letterName,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "يرجى رفع ملف PDF." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { error = "يُقبل ملفات PDF فقط." });

            if (level < 1 || level > 4)
                return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 4." });

            if (string.IsNullOrWhiteSpace(letter))
                return BadRequest(new { error = "يرجى إدخال الحرف." });

            try
            {
                var result = await educationalIngestionService.IngestAsync(
                    file.OpenReadStream(), file.FileName,
                    level, letter.Trim(), letterName?.Trim() ?? letter.Trim(),
                    ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "فشلت معالجة الكتاب.", detail = ex.Message });
            }
        }

        // ── Get RAG page chunks (admin viewer) ────────────────────────────────────
        [HttpGet("page-chunks")]
        [ProducesResponseType(typeof(List<RagPageChunkDto>), 200)]
        public async Task<IActionResult> GetPageChunks(
            [FromQuery] int? level,
            [FromQuery] string? letter)
        {
            var chunks = await pageChunkRepository.GetAllAsync(level, letter);
            var dtos = chunks.Select(c => new RagPageChunkDto(
                c.Id, c.SourceFile, c.PageNumber, c.Sentence, c.WordCount,
                c.ImagePath, c.Level, c.Letter, c.LetterName)).ToList();
            return Ok(dtos);
        }

        [HttpGet("documents")]
        [ProducesResponseType(typeof(List<KnowledgeDocumentDto>), 200)]
        public async Task<IActionResult> GetDocuments()
        {
            var docs = await db.KnowledgeDocuments
                .OrderByDescending(d => d.IngestedAt)
                .Select(d => new KnowledgeDocumentDto(
                    d.Id, d.FileName, d.DocumentType,
                    d.Letter, d.Level, d.Tags, d.ChunkCount, d.IngestedAt))
                .ToListAsync();
            return Ok(docs);
        }

        [HttpDelete("documents/{id:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            try { await ingestionService.DeleteAsync(id); return NoContent(); }
            catch (InvalidOperationException) { return NotFound(); }
        }

        [HttpPost("search")]
        [ProducesResponseType(typeof(List<RagSearchResult>), 200)]
        public async Task<IActionResult> Search([FromBody] string query)
        {
            var results = await queryService.SearchAsync(query, topK: 5);
            return Ok(results);
        }

        /// <summary>Upload a knowledge document (alias for ingest, called by admin-content).</summary>
        [HttpPost("documents")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(IngestDocumentResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UploadDocument(
            IFormFile file,
            [FromForm] string? name,
            [FromForm] string? description,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "يرجى رفع ملف." });

            var allowed = new[] { ".pdf", ".pptx", ".jpg", ".jpeg", ".png", ".webp", ".bmp" };
            var ext     = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new { error = $"نوع الملف '{ext}' غير مدعوم." });

            try
            {
                var request = new IngestDocumentRequest(null, null, description?.Trim());
                var result  = await ingestionService.IngestAsync(
                    file.OpenReadStream(), name ?? file.FileName, request, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("generate-lesson")]
        [ProducesResponseType(typeof(LessonDetailResponse), 200)]
        public async Task<IActionResult> GenerateLesson(
            [FromBody] GenerateLessonRequest request,
            [FromServices] LessonGenerationAgent lessonAgent,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Topic))
                return BadRequest(new { error = "يرجى إدخال موضوع الدرس." });

            var lesson = await lessonAgent.GenerateAsync(request, ct);
            return Ok(lesson);
        }
    }
}
