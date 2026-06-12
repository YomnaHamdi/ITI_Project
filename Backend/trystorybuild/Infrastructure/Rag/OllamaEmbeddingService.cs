using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Rag
{
    public class OllamaEmbeddingService(
        HttpClient httpClient,
        IOptions<RagSettings> settings,
        ILogger<OllamaEmbeddingService> logger) : IEmbeddingService
    {
        private readonly RagSettings _cfg = settings.Value;

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            var payload = new { model = _cfg.EmbeddingModel, prompt = text };

            var response = await httpClient.PostAsJsonAsync(
                $"{_cfg.OllamaEndpoint}/api/embeddings", payload);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
            logger.LogDebug("[Embedding] Got vector of size {Size}", result?.Embedding?.Length);
            return result?.Embedding ?? Array.Empty<float>();
        }

        private sealed class OllamaEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}
