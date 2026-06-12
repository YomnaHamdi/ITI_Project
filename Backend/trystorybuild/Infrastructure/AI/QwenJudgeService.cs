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
    public class QwenJudgeService(
    IChatClient chatClient,
    ILogger<QwenJudgeService> logger) : IJudgeService
    {
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        public async Task<JudgeResult> ValidateAsync(
            string title,
            List<string> sentences,
            List<string> imagePrompts)
        {
            var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AgentPrompts.JudgeSystemPrompt),
            new(ChatRole.User,   AgentPrompts.JudgeUserPrompt(title, sentences, imagePrompts))
        };

            logger.LogInformation("[Judge] Validating story: {Title}", title);

            try
            {
                var response = await chatClient.GetResponseAsync(messages);
                var raw = response.Text ?? string.Empty;
                var json = StripCodeFences(raw);

                var result = JsonSerializer.Deserialize<JudgeResult>(json, JsonOpts);

                if (result is null)
                {
                    logger.LogWarning("[Judge] Null result — defaulting to approved.");
                    return new JudgeResult(true, "تعذّر التحقق — تمت الموافقة تلقائياً");
                }

                logger.LogInformation("[Judge] Approved={Ok} Reason={Reason}", result.IsApproved, result.Reason);
                return result;
            }
            catch (Exception ex)
            {
                // Judge failure should not block story creation in MVP — log and approve
                logger.LogWarning(ex, "[Judge] Validation failed — defaulting to approved.");
                return new JudgeResult(true, "تعذّر التحقق — تمت الموافقة تلقائياً");
            }
        }

        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl = t.IndexOf('\n');
            var end = t.LastIndexOf("```");
            return nl >= 0 && end > nl ? t[(nl + 1)..end].Trim() : t;
        }
    }

}
