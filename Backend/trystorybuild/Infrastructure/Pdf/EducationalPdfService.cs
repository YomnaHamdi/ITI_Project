using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDFtoImage;
using SkiaSharp;
using System.Text.RegularExpressions;
using DomainPdfDoc  = Domain.Entities.PdfDocument;
using DomainPdfPage = Domain.Entities.PdfPage;
using PigDoc        = UglyToad.PdfPig.PdfDocument;

namespace Infrastructure.Pdf
{
    public class EducationalPdfService(
        AppDbContext db,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore,
        string contentRootPath,
        ILogger<EducationalPdfService> logger) : IEducationalPdfService
    {
        private string PdfsDir    => Path.Combine(contentRootPath, "Uploads", "PdfLibrary");
        private string ImagesBase => Path.Combine(contentRootPath, "wwwroot", "uploads", "pdf-pages");
        private const string ImagesUrlBase = "/uploads/pdf-pages";

        // ── Upload: extract text + images → save to DB ─────────────────────────
        public async Task<PdfDocumentDto> UploadAsync(
            Stream fileStream, string fileName,
            string letter, int level,
            CancellationToken ct = default)
        {
            Directory.CreateDirectory(PdfsDir);
            var docId   = Guid.NewGuid();
            var pdfPath = Path.Combine(PdfsDir, $"{docId}.pdf");

            await using (var fs = File.Create(pdfPath))
                await fileStream.CopyToAsync(fs, ct);

            logger.LogInformation("[PDF-LIB] Saved PDF → {Path}", pdfPath);

            var pages = await ExtractPagesAsync(pdfPath, docId, ct);
            var title = Path.GetFileNameWithoutExtension(fileName);

            var doc = new DomainPdfDoc
            {
                Id        = docId,
                Title     = title,
                FilePath  = pdfPath,
                Level     = level,
                Letter    = letter,
                PageCount = pages.Count,
            };
            doc.Pages.AddRange(pages);

            db.PdfDocuments.Add(doc);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("[PDF-LIB] Saved {Title} — {N} pages", title, pages.Count);
            return MapDto(doc);
        }

        // ── Generate embeddings: each page sentence → Chroma ──────────────────
        public async Task<EmbedResultDto> GenerateEmbeddingsAsync(
            Guid pdfId, CancellationToken ct = default)
        {
            var doc = await db.PdfDocuments
                .Include(d => d.Pages)
                .FirstOrDefaultAsync(d => d.Id == pdfId, ct)
                ?? throw new InvalidOperationException($"PDF {pdfId} not found.");

            await vectorStore.EnsureCollectionAsync();

            var pending = doc.Pages
                .Where(p => !p.IsEmbedded && !string.IsNullOrWhiteSpace(p.Sentence))
                .ToList();

            if (pending.Count == 0)
                return new EmbedResultDto(0, "لا توجد صفحات بانتظار التضمين.");

            int count = 0;
            foreach (var page in pending)
            {
                ct.ThrowIfCancellationRequested();
                logger.LogInformation("[PDF-LIB] Embedding {N}/{Total}", count + 1, pending.Count);

                var vector   = await embeddingService.GetEmbeddingAsync(page.Sentence);
                var chromaId = $"pdf_{pdfId:N}_p{page.PageNumber}";

                await vectorStore.AddChunksAsync(
                    [chromaId],
                    [vector],
                    [page.Sentence],
                    [new Dictionary<string, string>
                    {
                        ["source_file"]   = doc.Title,
                        ["letter"]        = doc.Letter,
                        ["level"]         = doc.Level.ToString(),
                        ["page_number"]   = page.PageNumber.ToString(),
                        ["image_url"]     = page.ImageUrl,
                        ["pdf_id"]        = pdfId.ToString(),
                        ["document_type"] = "educational-pdf",
                    }]);

                page.ChromaId  = chromaId;
                page.IsEmbedded = true;
                count++;
            }

            doc.EmbeddedPageCount   = doc.Pages.Count(p => p.IsEmbedded);
            doc.EmbeddingsGenerated = doc.Pages.All(p => p.IsEmbedded || string.IsNullOrWhiteSpace(p.Sentence));
            await db.SaveChangesAsync(ct);

            return new EmbedResultDto(count, $"تم توليد {count} تضمين بنجاح.");
        }

        // ── List / Detail / Delete / Stats ─────────────────────────────────────
        public async Task<List<PdfDocumentDto>> GetAllAsync() =>
            await db.PdfDocuments
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new PdfDocumentDto(
                    d.Id, d.Title, d.Letter, d.Level,
                    d.PageCount, d.EmbeddedPageCount, d.EmbeddingsGenerated,
                    d.UploadedAt))
                .ToListAsync();

        public async Task<PdfDocumentDetailDto> GetDetailAsync(Guid id)
        {
            var doc = await db.PdfDocuments
                .Include(d => d.Pages.OrderBy(p => p.PageNumber))
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new InvalidOperationException($"PDF {id} not found.");

            return new PdfDocumentDetailDto(
                doc.Id, doc.Title, doc.Letter, doc.Level,
                doc.PageCount, doc.EmbeddedPageCount, doc.EmbeddingsGenerated,
                doc.UploadedAt,
                doc.Pages.Select(p =>
                    new PdfPageDto(p.Id, p.PageNumber, p.Sentence, p.ImageUrl, p.IsEmbedded))
                    .ToList());
        }

        public async Task DeleteAsync(Guid id)
        {
            var doc = await db.PdfDocuments
                .Include(d => d.Pages)
                .FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new InvalidOperationException($"PDF {id} not found.");

            if (doc.Pages.Any(p => p.IsEmbedded))
            {
                try { await vectorStore.DeleteBySourceAsync(doc.Title); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[PDF-LIB] Chroma delete failed for {Id}", id);
                }
            }

            if (File.Exists(doc.FilePath)) File.Delete(doc.FilePath);

            var imgDir = Path.Combine(ImagesBase, id.ToString("N"));
            if (Directory.Exists(imgDir)) Directory.Delete(imgDir, recursive: true);

            db.PdfDocuments.Remove(doc);
            await db.SaveChangesAsync();
        }

        public async Task<PdfLibraryStatsDto> GetStatsAsync()
        {
            var totalPdfs     = await db.PdfDocuments.CountAsync();
            var totalPages    = await db.PdfPages.CountAsync();
            var totalEmbedded = await db.PdfPages.CountAsync(p => p.IsEmbedded);
            var lastUpdated   = await db.PdfDocuments.MaxAsync(d => (DateTime?)d.UploadedAt);
            return new PdfLibraryStatsDto(totalPdfs, totalPages, totalEmbedded, lastUpdated);
        }

        // ── Text + image extraction ────────────────────────────────────────────
        private async Task<List<DomainPdfPage>> ExtractPagesAsync(
            string pdfPath, Guid docId, CancellationToken ct)
        {
            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, ct);
            var imgDir   = Path.Combine(ImagesBase, docId.ToString("N"));
            Directory.CreateDirectory(imgDir);

            // Step 1: extract text per page using PdfPig
            var sentences = new Dictionary<int, string>();
            using (var pig = PigDoc.Open(pdfPath))
            {
                foreach (var page in pig.GetPages())
                {
                    var raw   = string.Join(" ", page.GetWords().Select(w => w.Text));
                    var clean = CleanPageText(raw);
                    sentences[page.Number] = clean;
                    logger.LogDebug("[PDF-LIB] Page {N} raw: {Raw}", page.Number, raw);
                    logger.LogInformation("[PDF-LIB] Page {N} sentence: {S}", page.Number, clean);
                }
            }

            // Step 2: render pages to PNG using PDFtoImage
            var pageCount = Conversion.GetPageCount(pdfBytes, password: null);
            var results   = new List<DomainPdfPage>(pageCount);

            for (int i = 0; i < pageCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                using var bitmap = Conversion.ToImage(
                    pdfBytes, page: i,
                    options: new(Dpi: 150, WithAnnotations: false, WithFormFill: false));

                var imgName = $"page_{i + 1}.png";
                var imgPath = Path.Combine(imgDir, imgName);

                using var skImage = SKImage.FromBitmap(bitmap);
                using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 90);
                await using (var stream = File.OpenWrite(imgPath))
                    encoded.SaveTo(stream);

                var imageUrl = $"{ImagesUrlBase}/{docId:N}/{imgName}";
                sentences.TryGetValue(i + 1, out var sentence);

                results.Add(new DomainPdfPage
                {
                    PdfDocumentId = docId,
                    PageNumber    = i + 1,
                    Sentence      = sentence ?? string.Empty,
                    ImageUrl      = imageUrl,
                });
            }

            return results;
        }

        // ── Text cleaning: remove footer/badge noise, deduplicate tracing line ─
        private static string CleanPageText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            var text = rawText;

            // Remove teacher name (المعلمة + 1–3 words)
            text = Regex.Replace(text, @"المعلمة\s+\S+(?:\s+\S+){0,2}", " ");
            // Remove level badge (المستوى + optional digit)
            text = Regex.Replace(text, @"المستوى\s*\d*", " ");
            // Remove standalone Latin chars (logo "AW" etc.)
            text = Regex.Replace(text, @"\b[A-Za-z]+\b", " ");
            // Remove standalone digits
            text = Regex.Replace(text, @"\b\d+\b", " ");
            // Collapse whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Deduplicate: tracing line repeats the sentence verbatim
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Even-length: check if first half == second half
            if (words.Length >= 2 && words.Length % 2 == 0)
            {
                int half   = words.Length / 2;
                var first  = string.Join(" ", words[..half]);
                var second = string.Join(" ", words[half..]);
                if (first == second) return first;
            }

            // Odd-length or non-halved: find shortest repeating prefix
            for (int n = 1; n < words.Length; n++)
            {
                var candidate = string.Join(" ", words[..n]);
                var rest      = string.Join(" ", words[n..]).Trim();
                if (rest == candidate || rest.StartsWith(candidate + " "))
                    return candidate;
            }

            return text;
        }

        private static PdfDocumentDto MapDto(DomainPdfDoc d) =>
            new(d.Id, d.Title, d.Letter, d.Level,
                d.PageCount, d.EmbeddedPageCount, d.EmbeddingsGenerated, d.UploadedAt);
    }
}
