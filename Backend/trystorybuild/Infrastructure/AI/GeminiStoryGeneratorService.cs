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
    public class GeminiStoryGeneratorService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiStoryGeneratorService> logger) : IStoryGeneratorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public async Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key is not configured.");

            var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
            var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var client = httpClientFactory.CreateClient("Gemini");

            var userText = AgentPrompts.StorySystemPrompt + "\n\n" +
                           AgentPrompts.StoryUserPrompt(childName, character, theme);

            var body = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = userText } } }
                },
                generationConfig = new { responseMimeType = "application/json" }
            };

            logger.LogInformation("[Gemini-Story] Generating story for {Child}...", childName);
            var resp = await client.PostAsJsonAsync(url, body);
            resp.EnsureSuccessStatusCode();

            var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
            var raw = envelope!.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            logger.LogDebug("[Gemini-Story] Raw: {Raw}", raw);

            var json = StripCodeFences(raw);
            try
            {
                var output = JsonSerializer.Deserialize<AiStoryOutput>(json, JsonOpts)
                    ?? throw new InvalidOperationException("Null story deserialization.");

                if (output.Pages.Count != 3)
                    throw new InvalidOperationException($"Expected 3 pages, got {output.Pages.Count}.");

                logger.LogInformation("[Gemini-Story] Generated {Count} pages.", output.Pages.Count);
                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[Gemini-Story] JSON parse failed. Raw:\n{Raw}", raw);
                throw new InvalidOperationException("Gemini story response was not valid JSON.", ex);
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
