using Application.DTOs;
using Application.Interfaces;
using Application.Mapping;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Pdf;

public class PdfImportService(
    IPdfPageRenderer pdfRenderer,
    IOcrService ocrService,
    IAiTextCleanupService cleanupService,
    ILessonRepository lessonRepository,   // was IStoryRepository
    IOptions<PdfImportSettings> settings,
    ILogger<PdfImportService> logger) : IPdfImportService
{
    public async Task<LessonDetailResponse> ImportBookAsync(
        int level,
        string letter,
        string letterName,
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

        var lesson = new Lesson
        {
            Id = lessonId,
            Level = level,
            Letter = letter.Trim(),
            LetterName = letterName.Trim(),
            Title = $"{letterName} — المستوى {level}"
        };

        for (var i = 0; i < imagePaths.Count; i++)
        {
            var pageNumber = i + 1;
            var isCover = pageNumber == 1;
            var webPath = ToWebPath(imagePaths[i]);

            string sentence;

            if (isCover)
            {
                sentence = letterName.Trim();
                lesson.CoverImagePath = webPath;
            }
            else
            {
                var rawOcr = await ocrService.ExtractArabicTextAsync(imagePaths[i]);
                sentence = string.IsNullOrWhiteSpace(rawOcr)
                    ? string.Empty
                    : await cleanupService.CleanupArabicSentenceAsync(rawOcr);

                if (string.IsNullOrWhiteSpace(sentence))
                    sentence = rawOcr.Trim();
            }

            lesson.Pages.Add(new LessonPage
            {
                LessonId = lessonId,
                PageNumber = pageNumber,
                Sentence = sentence,
                ImagePath = webPath,
                IsCoverPage = isCover,
                IsUnlocked = isCover || pageNumber == 2
            });
        }

        if (string.IsNullOrEmpty(lesson.CoverImagePath) && lesson.Pages.Count > 0)
            lesson.CoverImagePath = lesson.Pages[0].ImagePath;

        await lessonRepository.SaveAsync(lesson);

        logger.LogInformation("[PdfImport] Lesson {Id} created — {Count} pages", lesson.Id, lesson.Pages.Count);

        return LessonMapper.ToDetail(lesson);
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