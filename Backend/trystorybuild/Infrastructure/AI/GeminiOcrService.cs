using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.AI
{
    public class GeminiOcrService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GeminiOcrService> logger) : IOcrService
    {
        private const string OcrPrompt =
            """
            اقرأ النص العربي المكتوب في هذه الصورة بدقة.
            أخرج النص العربي فقط كما هو مكتوب، بدون أي شرح أو تعليق إضافي.
            إذا لم يوجد نص، أخرج نصاً فارغاً فقط.
            """;

        public async Task<string> ExtractArabicTextAsync(string imagePath)
        {
            logger.LogInformation("[Gemini-OCR] Processing: {Path}", imagePath);

            if (!File.Exists(imagePath))
            {
                logger.LogError("[Gemini-OCR] File not found: {Path}", imagePath);
                return string.Empty;
            }

            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("[Gemini-OCR] No API key configured.");
                return string.Empty;
            }

            try
            {
                var imageBytes  = await File.ReadAllBytesAsync(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);
                var mimeType    = DetectMimeType(imagePath);

                var model  = configuration["Gemini:VisionModel"] ?? "gemini-2.0-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");

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
                                new { text = OcrPrompt }
                            }
                        }
                    }
                };

                var resp = await client.PostAsJsonAsync(url, body);
                resp.EnsureSuccessStatusCode();

                var envelope = await resp.Content.ReadFromJsonAsync<JsonDocument>();
                var raw = envelope!.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                var cleaned = CleanArabicText(raw.Trim());
                logger.LogInformation("[Gemini-OCR] Extracted: '{Text}'", cleaned);
                return cleaned;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Gemini-OCR] Failed for {Path}", imagePath);
                return string.Empty;
            }
        }

        private static string DetectMimeType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".png"  => "image/png",
                ".webp" => "image/webp",
                ".gif"  => "image/gif",
                _       => "image/jpeg"
            };
        }

        private static string CleanArabicText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var chars = raw
                .Replace('\n', ' ')
                .Replace('\r', ' ')
                .Where(c =>
                    (c >= '؀' && c <= 'ۿ') ||
                    c == ' ' || c == '،' || c == '.')
                .ToArray();

            var result = new string(chars);
            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            return result.Trim();
        }
    }
}
