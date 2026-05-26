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
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme)
        {
            var messages = new List<ChatMessage>
        {
            new(ChatRole.System, StoryPrompts.SystemPrompt),
            new(ChatRole.User,   StoryPrompts.BuildUserPrompt(childName, character, theme))
        };

            logger.LogInformation(
                "Generating story — child: {Child}, character: {Char}, theme: {Theme}",
                childName, character, theme);

            // GetResponseAsync returns ChatResponse; .Text aggregates all content blocks
            var response = await chatClient.GetResponseAsync(messages);
            var rawText = response.Text ?? string.Empty;

            logger.LogDebug("Raw AI response: {Response}", rawText);

            var cleanJson = StripCodeFences(rawText);

            try
            {
                var output = JsonSerializer.Deserialize<AiStoryOutput>(cleanJson, JsonOptions)
                    ?? throw new InvalidOperationException("AI returned a null story object.");

                if (output.Pages.Count != 3)
                    throw new InvalidOperationException(
                        $"Expected exactly 3 pages but the AI returned {output.Pages.Count}.");

                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON parse failed.\nRaw:\n{Raw}", rawText);
                throw new InvalidOperationException("AI response was not valid JSON.", ex);
            }
        }

        /// <summary>
        /// Removes ```json ... ``` or ``` ... ``` fences some models add around JSON.
        /// </summary>
        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;

            var newlineIdx = t.IndexOf('\n');
            if (newlineIdx < 0) return t;

            var closingIdx = t.LastIndexOf("```");
            if (closingIdx > newlineIdx)
                return t[(newlineIdx + 1)..closingIdx].Trim();

            return t;
        }
    }

}
