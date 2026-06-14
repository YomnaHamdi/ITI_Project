using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Rag
{
    public interface IVisionDescriptionService
    {
        Task<string> DescribeArabicEducationalImageAsync(string imagePath);
    }

    public class GeminiVisionDescriptionService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiVisionDescriptionService> logger) : IVisionDescriptionService
    {
        private const string VisionPrompt =
            """
            أنت مساعد تعليمي للأطفال.
            انظر إلى هذه الصورة التعليمية وصفها بالتفصيل بالعربية.
            ركز على: الحرف العربي إن وجد، الكلمات المكتوبة، المحتوى المرئي، المفهوم التعليمي.
            اكتب وصفاً كاملاً باللغة العربية.
            """;

        public async Task<string> DescribeArabicEducationalImageAsync(string imagePath)
        {
            try
            {
                var apiKey = configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey)) return string.Empty;

                var imageBytes  = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);
                var mimeType    = Path.GetExtension(imagePath).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";

                var model = configuration["Gemini:VisionModel"] ?? "gemini-2.0-flash";
                var url   = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role  = "user",
                            parts = new object[]
                            {
                                new { inline_data = new { mime_type = mimeType, data = base64Image } },
                                new { text = VisionPrompt }
                            }
                        }
                    }
                };

                var resp = await httpClient.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
                var text = envelope!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                logger.LogInformation("[Gemini-Vision] Described: {Preview}",
                    text.Length > 80 ? text[..80] + "…" : text);
                return text.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Gemini-Vision] Failed to describe {Image}", imagePath);
                return string.Empty;
            }
        }
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
