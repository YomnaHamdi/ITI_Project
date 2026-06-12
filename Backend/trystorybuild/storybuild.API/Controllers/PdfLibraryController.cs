using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace storybuild.API.Controllers
{
    [ApiController]
    [Route("api/pdf-library")]
    public class PdfLibraryController(IEducationalPdfService svc) : ControllerBase
    {
        /// <summary>Upload a PDF, extract pages (text + images), save to DB.</summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromForm] string letter,
            [FromForm] int level,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "يرجى رفع ملف PDF." });

            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "يرجى رفع ملفات PDF فقط." });

            if (string.IsNullOrWhiteSpace(letter))
                return BadRequest(new { error = "يرجى تحديد الحرف." });

            if (level < 1 || level > 4)
                return BadRequest(new { error = "المستوى يجب أن يكون بين 1 و 4." });

            try
            {
                var result = await svc.UploadAsync(
                    file.OpenReadStream(), file.FileName, letter.Trim(), level, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Generate Chroma embeddings for all un-embedded pages in a PDF.</summary>
        [HttpPost("{id:guid}/embed")]
        public async Task<IActionResult> Embed(Guid id, CancellationToken ct)
        {
            try
            {
                var result = await svc.GenerateEmbeddingsAsync(id, ct);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>List all uploaded PDFs (summary).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await svc.GetAllAsync());

        /// <summary>Get a single PDF with all extracted pages.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            try   { return Ok(await svc.GetDetailAsync(id)); }
            catch { return NotFound(new { error = "الملف غير موجود." }); }
        }

        /// <summary>Delete PDF, its images, and Chroma embeddings.</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try   { await svc.DeleteAsync(id); return NoContent(); }
            catch { return NotFound(new { error = "الملف غير موجود." }); }
        }

        /// <summary>Library statistics (counts, last upload).</summary>
        [HttpGet("stats")]
        public async Task<IActionResult> Stats() =>
            Ok(await svc.GetStatsAsync());
    }
}
