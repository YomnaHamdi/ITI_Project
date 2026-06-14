using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.AI
{
    public class CloudflareSettings
    {
        public string AccountId       { get; set; } = string.Empty;
        public string ApiToken        { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = "wwwroot/images";
        public int    NumSteps        { get; set; } = 8;
        public int    Width           { get; set; } = 512;
        public int    Height          { get; set; } = 512;
    }

    public class CloudflareImageService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CloudflareImageService> logger) : IImageGenerationService
    {
        private const string Model = "@cf/black-forest-labs/flux-2-klein-9b";

        public async Task<string> GenerateImageAsync(string prompt, string fileName)
        {
            var accountId = configuration["Cloudflare:AccountId"];
            var apiToken  = configuration["Cloudflare:ApiToken"];
            var outDir    = configuration["Cloudflare:OutputDirectory"] ?? "wwwroot/images";

            if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(apiToken))
            {
                logger.LogWarning("[Cloudflare-Image] Missing AccountId or ApiToken — using placeholder.");
                return await WritePlaceholderAsync(Path.Combine(outDir, fileName), fileName);
            }

            Directory.CreateDirectory(outDir);
            var localPath = Path.Combine(outDir, fileName);

            try
            {
                var client = httpClientFactory.CreateClient("Cloudflare");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);

                var url  = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{Model}";
                var safePrompt = ContainsArabic(prompt)
                    ? "cartoon style, bright colors, child-friendly, cute animal characters in a sunny garden"
                    : "cartoon style, bright colors, child-friendly, " + prompt;

                var numSteps = int.TryParse(configuration["Cloudflare:NumSteps"], out var steps) ? steps : 8;
                var width    = int.TryParse(configuration["Cloudflare:Width"],    out var w)     ? w     : 512;
                var height   = int.TryParse(configuration["Cloudflare:Height"],   out var h)     ? h     : 512;

                var form = new MultipartFormDataContent();
                form.Add(new StringContent(safePrompt),        "prompt");
                form.Add(new StringContent(numSteps.ToString()), "num_steps");
                form.Add(new StringContent(width.ToString()),    "width");
                form.Add(new StringContent(height.ToString()),   "height");

                logger.LogInformation("[Cloudflare-Image] Generating — file:{File}", fileName);
                var resp = await client.PostAsync(url, form);
                resp.EnsureSuccessStatusCode();

                var result = await resp.Content.ReadFromJsonAsync<CfImageResponse>();
                if (result?.Result?.Image is null)
                    throw new InvalidOperationException("Cloudflare returned no image data.");

                var bytes = Convert.FromBase64String(result.Result.Image);
                await File.WriteAllBytesAsync(localPath, bytes);

                logger.LogInformation("[Cloudflare-Image] Saved → {Path}", localPath);
                return $"/images/{fileName}";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[Cloudflare-Image] Failed for {File} — using placeholder", fileName);
                return await WritePlaceholderAsync(localPath, fileName);
            }
        }

        private static bool ContainsArabic(string text) =>
            text.Any(c => c >= '؀' && c <= 'ۿ');

        private static async Task<string> WritePlaceholderAsync(string path, string fileName)
        {
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            await File.WriteAllBytesAsync(path, png);
            return $"/images/{fileName}";
        }

        private sealed class CfImageResponse
        {
            [JsonPropertyName("result")]  public CfImageResult? Result  { get; set; }
            [JsonPropertyName("success")] public bool           Success { get; set; }
        }

        private sealed class CfImageResult
        {
            [JsonPropertyName("image")] public string? Image { get; set; }
        }
    }
}
