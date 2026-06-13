using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Application.Agent
{
    public sealed class WritingCorrectionAgent(
        ILessonRepository lessonRepository,
        IWritingAttemptRepository writingAttemptRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WritingCorrectionAgent> logger)
    {
        private const double AcceptanceThreshold = 70.0;

        // ── Lesson-based evaluation (existing flow) ───────────────────────────────
        public async Task<WritingCorrectionResponse> EvaluateAsync(
            Guid lessonPageId,
            Guid lessonId,
            string childName,
            IFormFile image)
        {
            logger.LogInformation("[WritingAgent] Evaluating page {PageId} for {Child}", lessonPageId, childName);

            var uploadsFolder = Path.Combine("Uploads", "Writing");
            Directory.CreateDirectory(uploadsFolder);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var stream = File.Create(filePath))
                await image.CopyToAsync(stream);

            var lesson = await lessonRepository.GetByIdAsync(lessonId)
                ?? throw new InvalidOperationException($"Lesson {lessonId} not found.");
            var page = lesson.Pages.FirstOrDefault(p => p.Id == lessonPageId)
                ?? throw new InvalidOperationException($"LessonPage {lessonPageId} not found.");
            if (page.IsCoverPage)
                throw new InvalidOperationException("صفحة الغلاف لا تحتاج تمرين كتابة.");

            var expectedSentence = page.Sentence;
            var imageBytes       = await File.ReadAllBytesAsync(filePath);
            var base64           = Convert.ToBase64String(imageBytes);

            var (extractedText, similarity, feedback) = await EvaluateWithGeminiAsync(base64, expectedSentence);

            logger.LogInformation(
                "[WritingAgent] Extracted: '{Text}' | Similarity: {Score:F1}%",
                extractedText, similarity);

            var isAccepted = similarity >= AcceptanceThreshold;

            if (isAccepted)
            {
                var nextPage = lesson.Pages
                    .FirstOrDefault(p => p.PageNumber == page.PageNumber + 1);
                if (nextPage is not null)
                {
                    nextPage.IsUnlocked = true;
                    await lessonRepository.SaveAsync(lesson);
                    logger.LogInformation("[WritingAgent] Page {N} unlocked.", nextPage.PageNumber);
                }
            }

            await writingAttemptRepository.SaveAsync(new WritingAttempt
            {
                LessonPageId      = lessonPageId,
                ChildName         = childName,
                UploadedImagePath = filePath,
                ExtractedText     = extractedText,
                ExpectedSentence  = expectedSentence,
                SimilarityScore   = similarity,
                IsAccepted        = isAccepted
            });

            return new WritingCorrectionResponse(extractedText, expectedSentence, similarity, isAccepted, feedback);
        }

        // ── Standalone canvas evaluation (new) ────────────────────────────────────
        public async Task<WritingCorrectionResponse> EvaluateDirectAsync(
            string imageBase64,
            string expectedText)
        {
            logger.LogInformation("[WritingAgent] Direct canvas evaluation for: '{Expected}'", expectedText);
            var (extractedText, similarity, feedback) = await EvaluateWithGeminiAsync(imageBase64, expectedText);
            var isAccepted = similarity >= AcceptanceThreshold;
            return new WritingCorrectionResponse(extractedText, expectedText, similarity, isAccepted, feedback);
        }

        // ── Gemini 2.5 Flash vision ───────────────────────────────────────────────
        private async Task<(string extracted, double similarity, string feedback)>
            EvaluateWithGeminiAsync(string base64Image, string expectedSentence)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[WritingAgent] Gemini:ApiKey is not configured.");
                return (string.Empty, 0, $"مفتاح Gemini API غير مهيأ. الجملة المطلوبة: {expectedSentence}");
            }

            try
            {
                var prompt = $$"""
                    You are an Arabic handwriting evaluator for children.

                    The child was asked to write: "{{expectedSentence}}"

                    Look at the handwriting in the image.

                    Return ONLY valid JSON — no markdown, no extra text:
                    {
                      "detectedText": "",
                      "similarity": 0,
                      "differences": []
                    }

                    Rules:
                    - detectedText: exactly what you read from the image in Arabic, or "" if the canvas is empty
                    - similarity: integer 0-100 (how closely the writing matches the expected sentence)
                    - differences: short Arabic descriptions of specific mistakes; empty array [] if correct
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
                                new { inline_data = new { mime_type = "image/png", data = base64Image } }
                            }
                        }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                var model    = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url      = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client   = httpClientFactory.CreateClient("Gemini");
                var response = await client.PostAsJsonAsync(url, body);
                response.EnsureSuccessStatusCode();

                var raw = await response.Content.ReadAsStringAsync();
                logger.LogInformation("[WritingAgent] Gemini raw response: {Raw}", raw);

                using var rootDoc = JsonDocument.Parse(raw);
                var jsonText = rootDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                using var resultDoc = JsonDocument.Parse(jsonText);
                var detected = resultDoc.RootElement.TryGetProperty("detectedText", out var d)
                    ? (d.GetString() ?? string.Empty)
                    : string.Empty;
                var sim = resultDoc.RootElement.TryGetProperty("similarity", out var s)
                    ? Math.Clamp(s.GetDouble(), 0, 100)
                    : 0.0;

                var isAccepted = sim >= AcceptanceThreshold;
                var feedback = isAccepted
                    ? $"أحسنت! كتبت الجملة بدقة {sim:F0}٪ 🌟"
                    : $"حصلت على {sim:F0}٪ — حاول مرة أخرى! الجملة: {expectedSentence}";

                return (detected, sim, feedback);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[WritingAgent] Gemini evaluation failed.");
                return (string.Empty, 0, $"تعذّر تحليل الكتابة. الجملة: {expectedSentence}");
            }
        }

        // ── Local Levenshtein fallback (utility) ──────────────────────────────────
        private static double ComputeLocalSimilarity(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual)) return 0;
            var e = NormalizeArabic(expected);
            var a = NormalizeArabic(actual);
            if (e == a) return 100.0;
            int distance = Levenshtein(e, a);
            int maxLen   = Math.Max(e.Length, a.Length);
            return maxLen == 0 ? 100.0 : Math.Max(0, Math.Round((1.0 - (double)distance / maxLen) * 100.0, 1));
        }

        private static string NormalizeArabic(string text) =>
            new string(text
                .Where(c => !((c >= 'ً' && c <= 'ٟ') || c == 'ـ'))
                .ToArray())
            .Trim();

        private static int Levenshtein(string s, string t)
        {
            int m = s.Length, n = t.Length;
            var dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;
            for (int i = 1; i <= m; i++)
                for (int j = 1; j <= n; j++)
                    dp[i, j] = s[i - 1] == t[j - 1]
                        ? dp[i - 1, j - 1]
                        : 1 + Math.Min(dp[i - 1, j - 1],
                              Math.Min(dp[i - 1, j], dp[i, j - 1]));
            return dp[m, n];
        }
    }
}
