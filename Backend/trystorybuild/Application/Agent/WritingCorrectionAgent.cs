using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Application.Agent
{
    public sealed class WritingCorrectionAgent(
        ILessonRepository lessonRepository,
        IWritingAttemptRepository writingAttemptRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<WritingCorrectionAgent> logger)
    {
        private const double AcceptanceThreshold = 70.0;
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        private const string VisionModel = "minicpm-v";

        public async Task<WritingCorrectionResponse> EvaluateAsync(
            Guid lessonPageId,
            Guid lessonId,
            string childName,
            IFormFile image)
        {
            logger.LogInformation("[WritingAgent] Evaluating page {PageId} for {Child}",
                lessonPageId, childName);

            // ── Save image ─────────────────────────────────────────────────────
            var uploadsFolder = Path.Combine("Uploads", "Writing");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = File.Create(filePath))
                await image.CopyToAsync(stream);

            // ── Load lesson page ───────────────────────────────────────────────
            var lesson = await lessonRepository.GetByIdAsync(lessonId)
                ?? throw new InvalidOperationException($"Lesson {lessonId} not found.");

            var page = lesson.Pages.FirstOrDefault(p => p.Id == lessonPageId)
                ?? throw new InvalidOperationException($"LessonPage {lessonPageId} not found.");

            if (page.IsCoverPage)
                throw new InvalidOperationException("صفحة الغلاف لا تحتاج تمرين كتابة.");

            var expectedSentence = page.Sentence;

            // ── Vision evaluation ──────────────────────────────────────────────
            var (extractedText, similarity, feedback) =
                await EvaluateWithVisionAsync(filePath, expectedSentence);

            logger.LogInformation(
                "[WritingAgent] Extracted: '{Text}' | Similarity: {Score:F1}% | Feedback: {Feedback}",
                extractedText, similarity, feedback);

            var isAccepted = similarity >= AcceptanceThreshold;

            // ── Unlock next page ───────────────────────────────────────────────
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

            // ── Save attempt ───────────────────────────────────────────────────
            await writingAttemptRepository.SaveAsync(new WritingAttempt
            {
                LessonPageId = lessonPageId,
                ChildName = childName,
                UploadedImagePath = filePath,
                ExtractedText = extractedText,
                ExpectedSentence = expectedSentence,
                SimilarityScore = similarity,
                IsAccepted = isAccepted
            });

            return new WritingCorrectionResponse(
                extractedText, expectedSentence, similarity, isAccepted, feedback);
        }

        // ── minicpm-v via Ollama ───────────────────────────────────────────────

        private async Task<(string extracted, double similarity, string feedback)>
            EvaluateWithVisionAsync(string imagePath, string expectedSentence)
        {
            try
            {
                var imageBytes = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);

                var requestBody = new OllamaVisionRequest
                {
                    Model = VisionModel,
                    Prompt = BuildPrompt(expectedSentence),
                    Images = new[] { base64Image },
                    Stream = false
                };

                var client = httpClientFactory.CreateClient("Ollama");
                var response = await client.PostAsJsonAsync(OllamaUrl, requestBody);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaVisionResponse>();
                var rawAnswer = result?.Response?.Trim() ?? string.Empty;

                // Always log the raw response so problems are visible in the console
                logger.LogInformation(
                    "[VisionAgent] === RAW MODEL RESPONSE ===\n{Raw}\n=========================",
                    rawAnswer);

                return ParseVisionAnswer(rawAnswer, expectedSentence);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[VisionAgent] Vision evaluation failed.");
                return (
                    string.Empty,
                    0,
                    $"تعذّر تحليل الكتابة. حاول مرة أخرى. الجملة المطلوبة: {expectedSentence}"
                );
            }
        }

        // ── Prompt ────────────────────────────────────────────────────────────

        private static string BuildPrompt(string expectedSentence) => $"""
            You are a children's Arabic handwriting checker.

            The child was asked to write this Arabic sentence: {expectedSentence}

            Look at the image. It contains the child's handwriting on a white canvas.

            Your response MUST follow this exact format with no extra text:
            WRITTEN: <copy exactly what you see written in the image, or EMPTY if nothing is written>
            SCORE: <integer 0-100 showing how closely the handwriting matches: {expectedSentence}>
            NOTE: <one short Arabic sentence explaining what the child got right or wrong>

            Scoring guide:
            - All words written correctly = 90-100
            - Most words correct, minor mistakes = 70-89
            - Half the words correct = 40-69
            - Only one word written out of several = 20-39
            - Nothing written or completely unreadable = 0

            Example — sentence is "هذا خروف", child wrote "هذا خروف":
            WRITTEN: هذا خروف
            SCORE: 95
            NOTE: أحسنت! كتبت الجملة كاملة بشكل صحيح

            Example — sentence is "هذا خروف", child wrote only "خروف":
            WRITTEN: خروف
            SCORE: 50
            NOTE: كتبت خروف صح لكن نسيت كلمة هذا، أعد الكتابة

            Example — sentence is "هذا خروف", canvas is empty:
            WRITTEN: EMPTY
            SCORE: 0
            NOTE: لم تكتب أي شيء، اكتب الجملة كاملة
            """;

        // ── Parser ────────────────────────────────────────────────────────────

        private (string extracted, double similarity, string feedback)
            ParseVisionAnswer(string raw, string expectedSentence)
        {
            var extracted = string.Empty;
            var similarity = 0.0;
            var note = string.Empty;

            foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var t = line.Trim();

                if (t.StartsWith("WRITTEN:", StringComparison.OrdinalIgnoreCase))
                {
                    extracted = t["WRITTEN:".Length..].Trim();
                    if (extracted.Equals("EMPTY", StringComparison.OrdinalIgnoreCase))
                        extracted = string.Empty;
                }
                else if (t.StartsWith("SCORE:", StringComparison.OrdinalIgnoreCase))
                {
                    // Strip anything after the number: "95%", "95/100", "95 out of 100"
                    var numStr = new string(
                        t["SCORE:".Length..].Trim()
                         .TakeWhile(c => char.IsDigit(c) || c == '.')
                         .ToArray());

                    if (double.TryParse(numStr, out var parsed))
                        similarity = Math.Clamp(parsed, 0, 100);
                }
                else if (t.StartsWith("NOTE:", StringComparison.OrdinalIgnoreCase))
                {
                    note = t["NOTE:".Length..].Trim();
                }
            }

            // ── Fallback: model ignored the format ─────────────────────────────
            if (similarity == 0 && string.IsNullOrWhiteSpace(extracted))
            {
                logger.LogWarning(
                    "[VisionAgent] Could not parse structured response. " +
                    "Running local similarity on raw text as fallback.");

                var rawSimilarity = ComputeLocalSimilarity(expectedSentence, raw);
                if (rawSimilarity > 0)
                {
                    similarity = rawSimilarity;
                    extracted = raw.Length > 80 ? raw[..80] + "…" : raw;
                    note = $"تمت المقارنة تلقائياً. الجملة المطلوبة: {expectedSentence}";
                }
                else
                {
                    note = $"لم يتمكن النظام من قراءة الكتابة. الجملة: {expectedSentence}";
                }
            }

            // ── Fallback: score parsed but no note ─────────────────────────────
            if (string.IsNullOrWhiteSpace(note))
                note = similarity >= AcceptanceThreshold
                    ? "أحسنت!"
                    : $"حاول مرة أخرى! الجملة المطلوبة: {expectedSentence}";

            // ── Build the message shown to the student ─────────────────────────
            var message = similarity >= AcceptanceThreshold
                ? $"أحسنت! {note} ({similarity:F0}٪) 🌟"
                : $"حصلت على {similarity:F0}٪ — {note}";

            return (extracted, similarity, message);
        }

        // ── Local similarity fallback ─────────────────────────────────────────

        private static double ComputeLocalSimilarity(string expected, string actual)
        {
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(actual))
                return 0;

            var e = NormalizeArabic(expected);
            var a = NormalizeArabic(actual);

            if (e == a) return 100.0;

            int distance = Levenshtein(e, a);
            int maxLen = Math.Max(e.Length, a.Length);

            return maxLen == 0
                ? 100.0
                : Math.Max(0, Math.Round((1.0 - (double)distance / maxLen) * 100.0, 1));
        }

        private static string NormalizeArabic(string text) =>
            new string(text
                .Where(c => !((c >= '\u064B' && c <= '\u065F') || c == '\u0640'))
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

    // ── Ollama request / response DTOs ────────────────────────────────────────

    internal sealed class OllamaVisionRequest
    {
        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;
        [JsonPropertyName("prompt")] public string Prompt { get; set; } = string.Empty;
        [JsonPropertyName("images")] public string[] Images { get; set; } = Array.Empty<string>();
        [JsonPropertyName("stream")] public bool Stream { get; set; } = false;
    }

    internal sealed class OllamaVisionResponse
    {
        [JsonPropertyName("response")] public string Response { get; set; } = string.Empty;
    }
}