using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Agent
{
    public sealed class LessonGenerationAgent(
        IChatClient chatClient,
        IRagQueryService ragQueryService,
        ILessonRepository lessonRepository,
        ILogger<LessonGenerationAgent> logger)
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive    = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<LessonDetailResponse> GenerateAsync(
            GenerateLessonRequest request,
            CancellationToken ct = default)
        {
            logger.LogInformation("[LessonAgent] Generating lesson: {Topic} Level {Level}",
                request.Topic, request.Level);

            var query   = $"{request.Topic} {request.Letter} المستوى {request.Level}";
            var context = await ragQueryService.GetContextAsync(query, topK: 6);
            logger.LogInformation("[LessonAgent] RAG context length: {Len} chars", context.Length);

            var systemPrompt = BuildSystemPrompt(request.Level);
            var userPrompt   = BuildUserPrompt(request, context);

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User,   userPrompt)
            };

            var response  = await chatClient.GetResponseAsync(messages);
            var rawOutput = response.Text ?? string.Empty;
            logger.LogDebug("[LessonAgent] Raw output: {Raw}", rawOutput);

            var lessonOutput = ParseOutput(rawOutput, request);

            var lesson = new Lesson
            {
                Level      = request.Level,
                Letter     = request.Letter?.Trim() ?? "",
                LetterName = request.Topic.Trim(),
                Title      = lessonOutput.Title,
                CoverImagePath = ""
            };

            int pageNum = 1;
            foreach (var p in lessonOutput.Pages)
            {
                lesson.Pages.Add(new LessonPage
                {
                    LessonId    = lesson.Id,
                    PageNumber  = pageNum,
                    Sentence    = p.Sentence,
                    ImagePath   = "",
                    IsCoverPage = pageNum == 1,
                    IsUnlocked  = pageNum <= 2
                });
                pageNum++;
            }

            await lessonRepository.SaveAsync(lesson);
            logger.LogInformation("[LessonAgent] Lesson {Id} saved — {Pages} pages",
                lesson.Id, lesson.Pages.Count);

            return new LessonDetailResponse(
                lesson.Id, lesson.Level, lesson.Letter, lesson.LetterName, lesson.Title,
                lesson.CoverImagePath,
                lesson.Pages.OrderBy(p => p.PageNumber)
                    .Select(p => new LessonPageDto(
                        p.Id, p.PageNumber, p.Sentence, p.ImagePath, p.IsUnlocked, p.IsCoverPage))
                    .ToList());
        }

        private static string BuildSystemPrompt(int level) => $"""
            أنت معلم لغة عربية متخصص في تعليم الأطفال من عمر 3 إلى 6 سنوات.
            المستوى المطلوب: {level}
            القواعد: المستوى 1: جملة من كلمتين. المستوى 2: 3-4 كلمات. المستوى 3: 4-6 كلمات.
            أعد JSON فقط بدون أي نص إضافي.
            """;

        private static string BuildUserPrompt(GenerateLessonRequest req, string ragContext)
        {
            var contextSection = string.IsNullOrWhiteSpace(ragContext)
                ? ""
                : $"محتوى مناهج المعلمة:\n{ragContext}\n\n";
            var pageCount = req.Level switch { 1 => "4 إلى 5", 2 => "6 إلى 8", _ => "8 إلى 10" };

            return $$"""
                {{contextSection}}الموضوع: {{req.Topic}}
                الحرف المستهدف: {{req.Letter ?? "غير محدد"}}
                المستوى: {{req.Level}}

                أنشئ درساً من {{pageCount}} صفحات بهذا التنسيق JSON:
                {
                  "title": "عنوان الدرس",
                  "pages": [
                    {
                      "pageNumber": 1,
                      "sentence": "جملة عربية مناسبة للمستوى",
                      "imagePrompt": "cartoon style, bright colors, child-friendly: [English description]"
                    }
                  ]
                }
                """;
        }

        private AiLessonOutput ParseOutput(string raw, GenerateLessonRequest request)
        {
            var json = StripCodeFences(raw);
            try
            {
                return JsonSerializer.Deserialize<AiLessonOutput>(json, JsonOpts)
                    ?? FallbackOutput(request);
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[LessonAgent] JSON parse failed. Raw:\n{Raw}", raw);
                return FallbackOutput(request);
            }
        }

        private static AiLessonOutput FallbackOutput(GenerateLessonRequest request) => new(
            $"درس {request.Topic}",
            new List<AiLessonPage>
            {
                new(1, $"هذا {request.Letter}", "cartoon Arabic letter flashcard, bright colors")
            });

        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl  = t.IndexOf('\n');
            var end = t.LastIndexOf("```");
            return nl >= 0 && end > nl ? t[(nl + 1)..end].Trim() : t;
        }

        private record AiLessonOutput(string Title, List<AiLessonPage> Pages);
        private record AiLessonPage(int PageNumber, string Sentence, string ImagePrompt);
    }
}
