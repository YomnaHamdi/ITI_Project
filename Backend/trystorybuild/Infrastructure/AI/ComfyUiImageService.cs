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
        /// <summary>ComfyUI base URL, e.g. http://127.0.0.1:8188</summary>
        public string BaseUrl { get; set; } = "http://127.0.0.1:8188";

        /// <summary>Path to the workflow JSON template file.</summary>
        public string WorkflowPath { get; set; } = "workflow.json";

        /// <summary>Folder where .NET copies the finished image (served as static files).</summary>
        public string OutputDirectory { get; set; } = "wwwroot/images";

        /// <summary>How long to wait for ComfyUI to finish generating (seconds).</summary>
        public int PollingTimeoutSeconds { get; set; } = 180;

        /// <summary>How often to poll the history endpoint (milliseconds).</summary>
        public int PollingIntervalMs { get; set; } = 2000;
    }

    // ── ComfyUI response DTOs ─────────────────────────────────────────────────────
    internal sealed class ComfyPromptResponse
    {
        [JsonPropertyName("prompt_id")]
        public string PromptId { get; set; } = string.Empty;
    }

    // ── Service ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// Sends a workflow to ComfyUI's /prompt endpoint, polls /history/{prompt_id}
    /// until the image is ready, then downloads and saves it locally.
    /// </summary>
    public class ComfyUiImageService(
        HttpClient httpClient,
        IOptions<ComfyUiSettings> settings,
        ILogger<ComfyUiImageService> logger) : IImageGenerationService
    {
        private readonly ComfyUiSettings _cfg = settings.Value;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<string> GenerateImageAsync(string prompt, string fileName)
        {
            logger.LogInformation("[ComfyUI] Generating image — file: {File}", fileName);
            logger.LogDebug("[ComfyUI] Prompt: {Prompt}", prompt);

            Directory.CreateDirectory(_cfg.OutputDirectory);
            var localPath = Path.Combine(_cfg.OutputDirectory, fileName);

            try
            {
                // ── Step 1: load and patch the workflow template ──────────────────
                var workflowJson = await File.ReadAllTextAsync(_cfg.WorkflowPath);

                // Inject the positive prompt (node "1")
                workflowJson = workflowJson.Replace(
                    "\"YOUR_PROMPT\"",
                    JsonSerializer.Serialize(BuildChildFriendlyPrompt(prompt)));

                // Randomise seed so every generation is unique (node "6")
                var seed = Random.Shared.NextInt64(0, long.MaxValue);
                workflowJson = workflowJson.Replace("\"seed\": 0", $"\"seed\": {seed}");

                // ── Step 2: submit to ComfyUI /prompt ─────────────────────────────
                var workflowNode = JsonNode.Parse(workflowJson)!;
                var submitBody = new { prompt = workflowNode["prompt"] };

                var submitResponse = await httpClient.PostAsJsonAsync(
                    $"{_cfg.BaseUrl}/prompt", submitBody);

                submitResponse.EnsureSuccessStatusCode();

                var submitResult = await submitResponse.Content
                    .ReadFromJsonAsync<ComfyPromptResponse>(JsonOpts);

                if (string.IsNullOrWhiteSpace(submitResult?.PromptId))
                    throw new InvalidOperationException("ComfyUI did not return a prompt_id.");

                var promptId = submitResult.PromptId;
                logger.LogInformation("[ComfyUI] Submitted. prompt_id: {Id}", promptId);

                // ── Step 3: poll /history/{prompt_id} until done ──────────────────
                var imageName = await PollUntilCompleteAsync(promptId);

                // ── Step 4: download the image from ComfyUI /view ─────────────────
                var imageBytes = await httpClient.GetByteArrayAsync(
                    $"{_cfg.BaseUrl}/view?filename={imageName}&type=output");

                await File.WriteAllBytesAsync(localPath, imageBytes);
                logger.LogInformation("[ComfyUI] Image saved → {Path}", localPath);

                return $"/images/{fileName}";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[ComfyUI] Generation failed for {File} — using placeholder", fileName);
                return await WritePlaceholderAsync(localPath, fileName);
            }
        }

        // ── Polling ───────────────────────────────────────────────────────────────
        private async Task<string> PollUntilCompleteAsync(string promptId)
        {
            var deadline = DateTime.UtcNow.AddSeconds(_cfg.PollingTimeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(_cfg.PollingIntervalMs);

                var historyJson = await httpClient.GetStringAsync(
                    $"{_cfg.BaseUrl}/history/{promptId}");

                var history = JsonNode.Parse(historyJson);
                var entry = history?[promptId];

                if (entry is null) continue;   // not finished yet

                // ComfyUI marks completion when "outputs" is populated
                var outputs = entry["outputs"];
                if (outputs is null) continue;

                // Walk outputs to find the first image filename
                foreach (var nodeOutput in outputs.AsObject())
                {
                    var images = nodeOutput.Value?["images"]?.AsArray();
                    if (images is null || images.Count == 0) continue;

                    var imgName = images[0]?["filename"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(imgName))
                    {
                        logger.LogInformation("[ComfyUI] Image ready: {Name}", imgName);
                        return imgName;
                    }
                }
            }

            throw new TimeoutException(
                $"ComfyUI did not finish within {_cfg.PollingTimeoutSeconds} seconds.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private static string BuildChildFriendlyPrompt(string rawPrompt) =>
            $"cartoon style, child-friendly, bright cheerful colors, soft illustration, " +
            $"safe for children, {rawPrompt}";

        private static async Task<string> WritePlaceholderAsync(string localPath, string fileName)
        {
            var png = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");
            await File.WriteAllBytesAsync(localPath, png);
            return $"/images/{fileName}";
        }
    }

}
