using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Pdf;

public class PdfImportService(
    IPdfPageRenderer pdfRenderer,
    ILessonRepository lessonRepository,
    IOptions<PdfImportSettings> settings,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PdfImportService> logger) : IPdfImportService
{
    private const int MaxContentPages = 3;

    public async Task<LessonDetailResponse> ImportBookAsync(
        int level,
        string letter,
        string letterName,
        string title,
        IFormFile pdfFile,
        CancellationToken ct = default)
    {
        if (pdfFile.Length == 0)
            throw new InvalidOperationException("PDF file is empty.");

        var ext = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
        if (ext != ".pdf")
            throw new InvalidOperationException("Only PDF files are supported.");

        var cfg = settings.Value;
        Directory.CreateDirectory(cfg.PdfUploadDirectory);

        var lessonId = Guid.NewGuid();
        var pdfPath = Path.Combine(cfg.PdfUploadDirectory, $"{lessonId}.pdf");

        await using (var stream = File.Create(pdfPath))
            await pdfFile.CopyToAsync(stream, ct);

        logger.LogInformation("[PdfImport] Saved PDF → {Path}", pdfPath);

        var imagesDir = Path.Combine(cfg.ImagesOutputDirectory, lessonId.ToString());
        var imagePaths = await pdfRenderer.RenderPagesAsync(pdfPath, imagesDir, ct);

        if (imagePaths.Count == 0)
            throw new InvalidOperationException("PDF contains no pages.");

        var resolvedTitle = string.IsNullOrWhiteSpace(title)
            ? $"{letterName} — المستوى {level}"
            : title.Trim();

        var lesson = new Lesson
        {
            Id         = lessonId,
            Level      = level,
            Letter     = letter.Trim(),
            LetterName = letterName.Trim(),
            Title      = resolvedTitle
        };

        var contentPageCount = 0;

        for (var i = 0; i < imagePaths.Count; i++)
        {
            var pageNumber = i + 1;
            var isCover   = pageNumber == 1;
            var isLast    = pageNumber == imagePaths.Count && imagePaths.Count > 1;
            var webPath   = ToWebPath(imagePaths[i]);

            if (isLast)
            {
                logger.LogInformation("[PdfImport] Skipping last page ({N})", pageNumber);
                break;
            }

            string sentence;

            if (isCover)
            {
                sentence = letterName.Trim();
                lesson.CoverImagePath = webPath;
            }
            else
            {
                if (contentPageCount >= MaxContentPages)
                {
                    logger.LogInformation("[PdfImport] Reached {Max} content page limit — stopping.", MaxContentPages);
                    break;
                }

                sentence = await ExtractSentenceWithGeminiAsync(imagePaths[i], ct);
                contentPageCount++;
            }

            lesson.Pages.Add(new LessonPage
            {
                LessonId    = lessonId,
                PageNumber  = pageNumber,
                Sentence    = sentence,
                ImagePath   = webPath,
                IsCoverPage = isCover,
                IsUnlocked  = isCover || pageNumber == 2
            });
        }

        if (string.IsNullOrEmpty(lesson.CoverImagePath) && lesson.Pages.Count > 0)
            lesson.CoverImagePath = lesson.Pages[0].ImagePath;

        await lessonRepository.SaveAsync(lesson);

        logger.LogInformation("[PdfImport] Lesson {Id} created — {Count} pages", lesson.Id, lesson.Pages.Count);

        return LessonMapper.ToDetail(lesson);
    }

    private async Task<string> ExtractSentenceWithGeminiAsync(string imagePath, CancellationToken ct)
    {
        var apiKey = configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("[PdfImport] Gemini:ApiKey not configured — returning empty sentence.");
            return string.Empty;
        }

        try
        {
            var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
            var base64     = Convert.ToBase64String(imageBytes);
            var mimeType   = imagePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";

            var prompt = """
                You are an Arabic text extractor for children's educational books.
                Look at this page image and extract ONLY the Arabic sentence written on the page.
                Return ONLY the sentence text — no extra explanation, no punctuation from you.
                If the page has no Arabic text, return an empty string.
                """;

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { text = prompt },
                            new { inline_data = new { mime_type = mimeType, data = base64 } }
                        }
                    }
                }
            };

            var model    = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url      = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var client   = httpClientFactory.CreateClient("Gemini");
            var response = await client.PostAsJsonAsync(url, body, ct);
            response.EnsureSuccessStatusCode();

            var raw = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(raw);
            var sentence = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            logger.LogInformation("[PdfImport] Gemini extracted: '{Text}'", sentence.Trim());
            return sentence.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PdfImport] Gemini OCR failed for {Path}", imagePath);
            return string.Empty;
        }
    }

    private static string ToWebPath(string absolutePath)
    {
        var normalized = absolutePath.Replace('\\', '/');
        var idx = normalized.IndexOf("/wwwroot/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) return normalized[(idx + "/wwwroot".Length)..];
        idx = normalized.IndexOf("/images/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0) return normalized[idx..];
        return $"/images/lessons/{Path.GetFileName(normalized)}";
    }
}
