using Application.DTOs;
using Application.Interfaces;
using Application.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Infrastructure.AI
{
    public class QwenStoryGeneratorService(
    IChatClient chatClient,
    ILogger<QwenStoryGeneratorService> logger) : IStoryGeneratorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public async Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme)
        {
            var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AgentPrompts.StorySystemPrompt),
            new(ChatRole.User,   AgentPrompts.StoryUserPrompt(childName, character, theme))
        };

            logger.LogInformation("[Qwen-Story] Sending prompt to Qwen2.5:1.5b...");
            var response = await chatClient.GetResponseAsync(messages);
            var raw = response.Text ?? string.Empty;

            logger.LogDebug("[Qwen-Story] Raw response: {Raw}", raw);

            var json = StripCodeFences(raw);

            try
            {
                var output = JsonSerializer.Deserialize<AiStoryOutput>(json, JsonOpts)
                    ?? throw new InvalidOperationException("Null deserialization result.");

                if (output.Pages.Count != 3)
                    throw new InvalidOperationException($"Expected 3 pages, got {output.Pages.Count}.");

                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[Qwen-Story] JSON parse failed.\nRaw:\n{Raw}", raw);
                throw new InvalidOperationException("AI story response was not valid JSON.", ex);
            }
        }

        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl = t.IndexOf('\n');
            if (nl < 0) return t;
            var end = t.LastIndexOf("```");
            return end > nl ? t[(nl + 1)..end].Trim() : t;
        }
    }

}
