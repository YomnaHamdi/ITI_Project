using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Infrastructure.AI
{
    // ── Settings ──────────────────────────────────────────────────────────────────
    public class ComfyUiSettings
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:8188";
        public string WorkflowPath { get; set; } = "workflow.json";
        public string OutputDirectory { get; set; } = "wwwroot/images";
        public int PollingTimeoutSeconds { get; set; } = 300;
        public int PollingIntervalMs { get; set; } = 2000;
    }

    internal sealed class ComfyPromptResponse
    {
        [JsonPropertyName("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
    }

    public class ComfyUiImageService(
        HttpClient httpClient,
        IOptions<ComfyUiSettings> settings,
        ILogger<ComfyUiImageService> logger) : IImageGenerationService
    {
        private readonly ComfyUiSettings _cfg = settings.Value;

        public async Task<string> GenerateImageAsync(string prompt, string fileName)
        {
            logger.LogInformation("[ComfyUI] Generating image — file:{File}", fileName);
            Directory.CreateDirectory(_cfg.OutputDirectory);
            var localPath = Path.Combine(_cfg.OutputDirectory, fileName);

            try
            {
                // Load and patch workflow template
                var workflowJson = await File.ReadAllTextAsync(_cfg.WorkflowPath);
                workflowJson = workflowJson.Replace(
                    "\"YOUR_PROMPT\"",
                    JsonSerializer.Serialize("cartoon style, child-friendly, bright colors, " + prompt));

                // Randomise seed
                workflowJson = workflowJson.Replace(
                    "\"seed\": 0",
                    $"\"seed\": {Random.Shared.NextInt64(0, long.MaxValue)}");

                // Submit to ComfyUI
                var workflowNode = JsonNode.Parse(workflowJson)!;
                var submitResponse = await httpClient.PostAsJsonAsync(
                    $"{_cfg.BaseUrl}/prompt",
                    new { prompt = workflowNode["prompt"] });

                submitResponse.EnsureSuccessStatusCode();

                var submitted = await submitResponse.Content
                    .ReadFromJsonAsync<ComfyPromptResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (string.IsNullOrWhiteSpace(submitted?.PromptId))
                    throw new InvalidOperationException("ComfyUI returned no prompt_id.");

                logger.LogInformation("[ComfyUI] Submitted — prompt_id:{Id}", submitted.PromptId);

                // Poll history
                var imageName = await PollAsync(submitted.PromptId);

                // Download image
                var bytes = await httpClient.GetByteArrayAsync(
                    $"{_cfg.BaseUrl}/view?filename={imageName}&type=output");
                await File.WriteAllBytesAsync(localPath, bytes);

                logger.LogInformation("[ComfyUI] Image saved → {Path}", localPath);
                return $"/images/{fileName}";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[ComfyUI] Failed for {File} — using placeholder", fileName);
                return await WritePlaceholderAsync(localPath, fileName);
            }
        }

        private async Task<string> PollAsync(string promptId)
        {
            var deadline = DateTime.UtcNow.AddSeconds(_cfg.PollingTimeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(_cfg.PollingIntervalMs);
                var histJson = await httpClient.GetStringAsync($"{_cfg.BaseUrl}/history/{promptId}");
                var history = JsonNode.Parse(histJson);
                var outputs = history?[promptId]?["outputs"];
                if (outputs is null) continue;

                foreach (var node in outputs.AsObject())
                {
                    var images = node.Value?["images"]?.AsArray();
                    if (images is null || images.Count == 0) continue;
                    var name = images[0]?["filename"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
            throw new TimeoutException($"ComfyUI timed out after {_cfg.PollingTimeoutSeconds}s.");
        }

        private static async Task<string> WritePlaceholderAsync(string path, string fileName)
        {
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            await File.WriteAllBytesAsync(path, png);
            return $"/images/{fileName}";
        }
    }
}
