using Application.DTOs;
using Application.Interfaces;
using Application.Prompts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.AI
{
    public class GeminiExamGeneratorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiExamGeneratorService> logger) : IExamGeneratorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
        };

        public Task<AiExamOutput> GenerateAsync(string storyText) =>
            CallGeminiAsync(AgentPrompts.ExamSystemPrompt, storyText, "Exam");

        public Task<AiExamOutput> GenerateLessonAsync(string lessonText) =>
            CallGeminiAsync(AgentPrompts.LessonExamSystemPrompt, lessonText, "LessonExam");

        // ── Core Gemini call ──────────────────────────────────────────────────────
        private async Task<AiExamOutput> CallGeminiAsync(string systemPrompt, string userText, string tag)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Gemini-{Tag}] Gemini:ApiKey not configured.", tag);
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            logger.LogInformation("[Gemini-{Tag}] Generating exam questions...", tag);

            var body = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = systemPrompt + "\n\n" + userText } } }
                },
                generationConfig = new { responseMimeType = "application/json" }
            };

            var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var client = httpClientFactory.CreateClient("Gemini");

            var resp = await client.PostAsJsonAsync(url, body);
            resp.EnsureSuccessStatusCode();

            // Unwrap Gemini envelope → get the text content
            var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
            var raw = envelope!.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            logger.LogDebug("[Gemini-{Tag}] Raw: {Raw}", tag, raw);

            var json = SanitizeJson(StripCodeFences(raw));

            try
            {
                var output = JsonSerializer.Deserialize<AiExamOutput>(json, JsonOpts)
                    ?? throw new InvalidOperationException("Null exam deserialization.");

                if (output.Questions.Count == 0)
                    throw new InvalidOperationException("Gemini returned 0 exam questions.");

                logger.LogInformation("[Gemini-{Tag}] Parsed {Count} questions.", tag, output.Questions.Count);
                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[Gemini-{Tag}] JSON parse failed. Raw:\n{Raw}", tag, raw);
                throw new InvalidOperationException("Gemini exam response was not valid JSON.", ex);
            }
        }

        // ── Helpers (same as before) ──────────────────────────────────────────────
        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl  = t.IndexOf('\n');
            var end = t.LastIndexOf("```");
            return nl >= 0 && end > nl ? t[(nl + 1)..end].Trim() : t;
        }

        private static string SanitizeJson(string text)
        {
            var sb     = new System.Text.StringBuilder(text.Length);
            bool inStr = false, escape = false;

            foreach (char c in text)
            {
                if (escape) { sb.Append(c); escape = false; continue; }
                if (c == '\\' && inStr) { escape = true; sb.Append(c); continue; }
                if (c == '"') { inStr = !inStr; sb.Append(c); continue; }

                if (inStr)
                {
                    switch (c)
                    {
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:   sb.Append(c);     break;
                    }
                }
                else { sb.Append(c); }
            }

            return sb.ToString();
        }
    }
}
