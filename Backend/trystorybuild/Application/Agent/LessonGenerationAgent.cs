using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Agent
{
    public sealed class LessonGenerationAgent(
        IRagQueryService ragQueryService,
        ILessonRepository lessonRepository,
        ILevelWordConfigRepository wordConfigRepository,
        ILessonAssignmentRepository assignmentRepository,
        IImageGenerationService imageService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LessonGenerationAgent> logger)
    {
        private const int MaxPages = 3;

        public async Task<LessonDetailResponse> GenerateAsync(
            GenerateLessonRequest request,
            CancellationToken ct = default)
        {
            logger.LogInformation("[LessonAgent] Generating: '{Topic}' Level {Level}", request.Topic, request.Level);

            // 1. Get word count for this level
            var wordCount = await wordConfigRepository.GetWordCountAsync(request.Level);
            logger.LogInformation("[LessonAgent] Word count for level {L}: {W}", request.Level, wordCount);

            // 2. RAG: get nearest sentences from similar lessons
            var query   = $"{request.Topic} {request.Letter ?? ""} المستوى {request.Level}";
            var context = await ragQueryService.GetContextAsync(query, topK: 5);
            logger.LogInformation("[LessonAgent] RAG context: {Len} chars", context.Length);

            // 3. Gemini generates sentences + image prompts
            var pages = await GenerateWithGeminiAsync(request, wordCount, context, ct);

            // 4. ComfyUI generates images for each page
            var lesson = new Lesson
            {
                Level       = request.Level,
                Letter      = request.Letter?.Trim() ?? string.Empty,
                LetterName  = request.Topic.Trim(),
                Title       = $"درس {request.Topic} — المستوى {request.Level}",
                IsGenerated = true,
                CreatorId   = request.CreatorId,
                CreatorRole = request.CreatorRole,
                PromptText  = request.Topic
            };

            int pageNum = 1;
            foreach (var p in pages.Take(MaxPages))
            {
                ct.ThrowIfCancellationRequested();
                var imagePath = string.Empty;
                if (!string.IsNullOrWhiteSpace(p.ImagePrompt))
                {
                    try
                    {
                        var fileName = $"gen_{lesson.Id}_{pageNum}.png";
                        imagePath = await imageService.GenerateImageAsync(p.ImagePrompt, fileName);
                        logger.LogInformation("[LessonAgent] Image for page {N}: {Path}", pageNum, imagePath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[LessonAgent] Image gen failed for page {N}", pageNum);
                    }
                }

                lesson.Pages.Add(new LessonPage
                {
                    LessonId    = lesson.Id,
                    PageNumber  = pageNum,
                    Sentence    = p.Sentence,
                    ImagePath   = imagePath,
                    ImagePrompt = p.ImagePrompt,
                    IsCoverPage = pageNum == 1,
                    IsUnlocked  = pageNum <= 2
                });
                pageNum++;
            }

            if (lesson.Pages.Count > 0)
                lesson.CoverImagePath = lesson.Pages[0].ImagePath;

            await lessonRepository.SaveAsync(lesson);
            logger.LogInformation("[LessonAgent] Lesson {Id} saved — {Pages} pages", lesson.Id, lesson.Pages.Count);

            // 5. Create assignment if target specified
            if (request.CreatorId.HasValue && (request.TargetStudentId.HasValue || request.TargetGroupId.HasValue))
            {
                var assignment = new LessonAssignment
                {
                    LessonId        = lesson.Id,
                    TeacherId       = request.CreatorId.Value,
                    TargetType      = request.TargetStudentId.HasValue ? "Student" : "Group",
                    TargetStudentId = request.TargetStudentId,
                    TargetGroupId   = request.TargetGroupId
                };
                await assignmentRepository.SaveAsync(assignment);
                logger.LogInformation("[LessonAgent] Assignment created → {Type}", assignment.TargetType);
            }

            return ToDetail(lesson);
        }

        // ── Gemini: generate sentences + image prompts ────────────────────────────
        private async Task<List<GeminiPage>> GenerateWithGeminiAsync(
            GenerateLessonRequest request, int wordCount, string ragContext, CancellationToken ct)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[LessonAgent] Gemini:ApiKey not configured.");
                return FallbackPages(request);
            }

            try
            {
                var contextSection = string.IsNullOrWhiteSpace(ragContext) ? "" :
                    $"أمثلة جمل من كتب مشابهة:\n{ragContext}\n\n";

                var prompt = $$"""
                    {{contextSection}}أنت معلم لغة عربية للأطفال من عمر 3-6 سنوات.
                    الموضوع: {{request.Topic}}
                    الحرف المستهدف: {{request.Letter ?? "غير محدد"}}
                    المستوى: {{request.Level}}
                    عدد الكلمات في كل جملة: {{wordCount}} كلمة بالضبط.

                    أنشئ {{MaxPages}} صفحات دراسية. لكل صفحة:
                    - جملة عربية بسيطة للأطفال تحتوي على {{wordCount}} كلمة بالضبط
                    - وصف إنجليزي للصورة الكرتونية (cartoon style, bright colors, child-friendly)

                    أعد JSON فقط بدون أي نص إضافي:
                    {
                      "pages": [
                        {
                          "sentence": "جملة عربية",
                          "imagePrompt": "english description for cartoon image"
                        }
                      ]
                    }
                    """;

                var body = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");
                var resp   = await client.PostAsJsonAsync(url, body, ct);
                resp.EnsureSuccessStatusCode();

                var raw = await resp.Content.ReadAsStringAsync(ct);
                logger.LogInformation("[LessonAgent] Gemini raw: {Raw}", raw);

                using var rootDoc = JsonDocument.Parse(raw);
                var jsonText = rootDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                using var resultDoc = JsonDocument.Parse(jsonText);
                var pagesEl = resultDoc.RootElement.GetProperty("pages");

                var result = new List<GeminiPage>();
                foreach (var pageEl in pagesEl.EnumerateArray())
                {
                    var sentence    = pageEl.TryGetProperty("sentence",    out var s) ? s.GetString() ?? "" : "";
                    var imagePrompt = pageEl.TryGetProperty("imagePrompt", out var ip) ? ip.GetString() ?? "" : "";
                    if (!string.IsNullOrWhiteSpace(sentence))
                        result.Add(new GeminiPage(sentence, imagePrompt));
                }

                return result.Count > 0 ? result : FallbackPages(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[LessonAgent] Gemini generation failed.");
                return FallbackPages(request);
            }
        }

        private static List<GeminiPage> FallbackPages(GenerateLessonRequest request) =>
            Enumerable.Range(1, MaxPages)
                .Select(i => new GeminiPage(
                    $"هذا {request.Letter ?? "حرف"} رقم {i}",
                    $"cartoon Arabic letter {request.Letter ?? "alef"}, bright colors, child-friendly, page {i}"))
                .ToList();

        private static LessonDetailResponse ToDetail(Lesson lesson) =>
            new(lesson.Id, lesson.Level, lesson.Letter, lesson.LetterName, lesson.Title,
                lesson.CoverImagePath,
                lesson.Pages.OrderBy(p => p.PageNumber)
                    .Select(p => new LessonPageDto(p.Id, p.PageNumber, p.Sentence, p.ImagePath, p.IsUnlocked, p.IsCoverPage))
                    .ToList());

        private record GeminiPage(string Sentence, string ImagePrompt);
    }
}
