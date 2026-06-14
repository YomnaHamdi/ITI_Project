using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Rag
{
    public class GeminiEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiEmbeddingService> logger) : IEmbeddingService
    {
        private const string EmbeddingModel = "gemini-embedding-004";

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Gemini-Embed] No API key configured.");
                return Array.Empty<float>();
            }

            var url  = $"https://generativelanguage.googleapis.com/v1beta/models/{EmbeddingModel}:embedContent?key={apiKey}";
            var body = new
            {
                model   = $"models/{EmbeddingModel}",
                content = new { parts = new[] { new { text } } }
            };

            var resp = await httpClient.PostAsJsonAsync(url, body);
            resp.EnsureSuccessStatusCode();

            var result = await resp.Content.ReadFromJsonAsync<GeminiEmbedResponse>();
            var values = result?.Embedding?.Values ?? Array.Empty<float>();
            logger.LogDebug("[Gemini-Embed] Got vector of size {Size}", values.Length);
            return values;
        }

        private sealed class GeminiEmbedResponse
        {
            [JsonPropertyName("embedding")] public GeminiEmbedding? Embedding { get; set; }
        }

        private sealed class GeminiEmbedding
        {
            [JsonPropertyName("values")] public float[] Values { get; set; } = Array.Empty<float>();
        }
    }
}
