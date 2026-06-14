using Application.Interfaces;
using Application.Prompts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.AI
{
    public class GeminiTextCleanupService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiTextCleanupService> logger) : IAiTextCleanupService
    {
        public async Task<string> CleanupArabicSentenceAsync(string ocrText)
        {
            if (string.IsNullOrWhiteSpace(ocrText))
                return string.Empty;

            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Gemini-Cleanup] No API key — returning raw text.");
                return ocrText.Trim();
            }

            try
            {
                var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");

                var userText = AgentPrompts.OcrCleanupSystemPrompt + "\n\n" +
                               AgentPrompts.OcrCleanupUserPrompt(ocrText);

                var body = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = userText } } }
                    }
                };

                logger.LogInformation("[Gemini-Cleanup] Cleaning: '{Text}'", ocrText);
                var resp = await client.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
                var cleaned = envelope!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                cleaned = cleaned.Trim().Trim('"', '\'', '«', '»');

                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    logger.LogWarning("[Gemini-Cleanup] Empty result — using raw OCR.");
                    return ocrText.Trim();
                }

                logger.LogInformation("[Gemini-Cleanup] Result: '{Text}'", cleaned);
                return cleaned;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Gemini-Cleanup] Failed — returning raw text.");
                return ocrText.Trim();
            }
        }
    }
}
