using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Rag
{
    public interface IVisionDescriptionService
    {
        Task<string> DescribeArabicEducationalImageAsync(string imagePath);
    }

    public class OllamaVisionDescriptionService(
        HttpClient httpClient,
        IOptions<RagSettings> settings,
        ILogger<OllamaVisionDescriptionService> logger) : IVisionDescriptionService
    {
        private readonly RagSettings _cfg = settings.Value;

        public async Task<string> DescribeArabicEducationalImageAsync(string imagePath)
        {
            try
            {
                var imageBytes  = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);

                var payload = new OllamaVisionRequest
                {
                    Model  = _cfg.VisionModel,
                    Prompt = """
                        أنت مساعد تعليمي للأطفال.
                        انظر إلى هذه الصورة التعليمية وصفها بالتفصيل بالعربية.
                        ركز على: الحرف العربي إن وجد، الكلمات المكتوبة، المحتوى المرئي، المفهوم التعليمي.
                        اكتب وصفاً كاملاً باللغة العربية.
                        """,
                    Images = new[] { base64Image },
                    Stream = false
                };

                var response = await httpClient.PostAsJsonAsync(
                    $"{_cfg.OllamaEndpoint}/api/generate", payload);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaVisionResponse>();
                var text   = result?.Response?.Trim() ?? string.Empty;
                logger.LogInformation("[Vision-RAG] Described image: {Preview}",
                    text.Length > 80 ? text[..80] + "…" : text);
                return text;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Vision-RAG] Failed to describe {Image}", imagePath);
                return string.Empty;
            }
        }

        private sealed class OllamaVisionRequest
        {
            [JsonPropertyName("model")]  public string   Model  { get; set; } = string.Empty;
            [JsonPropertyName("prompt")] public string   Prompt { get; set; } = string.Empty;
            [JsonPropertyName("images")] public string[] Images { get; set; } = Array.Empty<string>();
            [JsonPropertyName("stream")] public bool     Stream { get; set; } = false;
        }

        private sealed class OllamaVisionResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; } = string.Empty;
        }
    }
}
