using Application.Interfaces;
using Application.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Infrastructure.AI;

public class QwenOcrCleanupService(
    IChatClient chatClient,
    ILogger<QwenOcrCleanupService> logger) : IAiTextCleanupService
{
    public async Task<string> CleanupArabicSentenceAsync(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return string.Empty;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AgentPrompts.OcrCleanupSystemPrompt),
            new(ChatRole.User, AgentPrompts.OcrCleanupUserPrompt(ocrText))
        };

        logger.LogInformation("[Qwen-OCR-Cleanup] Cleaning: '{Text}'", ocrText);
        var response = await chatClient.GetResponseAsync(messages);
        var cleaned = (response.Text ?? string.Empty).Trim().Trim('"', '\'', '«', '»');

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            logger.LogWarning("[Qwen-OCR-Cleanup] Empty cleanup result — using raw OCR.");
            return ocrText.Trim();
        }

        logger.LogInformation("[Qwen-OCR-Cleanup] Result: '{Text}'", cleaned);
        return cleaned;
    }
}
