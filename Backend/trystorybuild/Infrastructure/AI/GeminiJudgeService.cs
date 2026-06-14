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
    public class GeminiJudgeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiJudgeService> logger) : IJudgeService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public async Task<JudgeResult> ValidateAsync(
            string title,
            List<string> sentences,
            List<string> imagePrompts)
        {
            try
            {
                var apiKey = configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                    return new JudgeResult(true, "Gemini API key not configured — approved by default.");

                var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");

                var userText = AgentPrompts.JudgeSystemPrompt + "\n\n" +
                               AgentPrompts.JudgeUserPrompt(title, sentences, imagePrompts);

                var body = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = userText } } }
                    },
                    generationConfig = new { responseMimeType = "application/json" }
                };

                logger.LogInformation("[Gemini-Judge] Validating story: {Title}", title);
                var resp = await client.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
                var raw = envelope!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "{}";

                var json   = StripCodeFences(raw);
                var result = JsonSerializer.Deserialize<JudgeResult>(json, JsonOpts);

                if (result is null)
                {
                    logger.LogWarning("[Gemini-Judge] Null result — approved by default.");
                    return new JudgeResult(true, "تعذّر التحقق — تمت الموافقة تلقائياً");
                }

                logger.LogInformation("[Gemini-Judge] Approved={Ok} Reason={Reason}", result.IsApproved, result.Reason);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Gemini-Judge] Validation failed — approved by default.");
                return new JudgeResult(true, "تعذّر التحقق — تمت الموافقة تلقائياً");
            }
        }

        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl  = t.IndexOf('\n');
            var end = t.LastIndexOf("```");
            return nl >= 0 && end > nl ? t[(nl + 1)..end].Trim() : t;
        }
    }
}
