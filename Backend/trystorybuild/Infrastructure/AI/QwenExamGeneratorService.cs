using Application.DTOs;
using Application.Interfaces;
using Application.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.AI
{
    public class QwenExamGeneratorService(
        IChatClient chatClient,
        ILogger<QwenExamGeneratorService> logger) : IExamGeneratorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive    = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<AiExamOutput> GenerateAsync(string storyText)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, AgentPrompts.ExamSystemPrompt),
                new(ChatRole.User,   storyText)
            };

            logger.LogInformation("[Qwen-Exam] Generating 4 mixed questions...");
            var response = await chatClient.GetResponseAsync(messages);
            var raw      = response.Text ?? string.Empty;
            logger.LogDebug("[Qwen-Exam] Raw: {Raw}", raw);

            var json = SanitizeJson(StripCodeFences(raw));

            try
            {
                var output = JsonSerializer.Deserialize<AiExamOutput>(json, JsonOpts)
                    ?? throw new InvalidOperationException("Null exam deserialization.");

                if (output.Questions.Count == 0)
                    throw new InvalidOperationException("AI returned 0 exam questions.");

                logger.LogInformation("[Qwen-Exam] Parsed {Count} questions.", output.Questions.Count);
                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[Qwen-Exam] JSON parse failed. Raw:\n{Raw}", raw);
                throw new InvalidOperationException("AI exam response was not valid JSON.", ex);
            }
        }

        public async Task<AiExamOutput> GenerateLessonAsync(string lessonText)
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, AgentPrompts.LessonExamSystemPrompt),
                new(ChatRole.User,   lessonText)
            };

            logger.LogInformation("[Qwen-LessonExam] Generating simple lesson questions...");
            var response = await chatClient.GetResponseAsync(messages);
            var raw      = response.Text ?? string.Empty;
            logger.LogDebug("[Qwen-LessonExam] Raw: {Raw}", raw);

            var json = SanitizeJson(StripCodeFences(raw));

            try
            {
                var output = JsonSerializer.Deserialize<AiExamOutput>(json, JsonOpts)
                    ?? throw new InvalidOperationException("Null lesson exam deserialization.");

                if (output.Questions.Count == 0)
                    throw new InvalidOperationException("AI returned 0 lesson exam questions.");

                logger.LogInformation("[Qwen-LessonExam] Parsed {Count} questions.", output.Questions.Count);
                return output;
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "[Qwen-LessonExam] JSON parse failed. Raw:\n{Raw}", raw);
                throw new InvalidOperationException("AI lesson exam response was not valid JSON.", ex);
            }
        }

        private static string StripCodeFences(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```")) return t;
            var nl  = t.IndexOf('\n');
            var end = t.LastIndexOf("```");
            return nl >= 0 && end > nl ? t[(nl + 1)..end].Trim() : t;
        }

        // Escapes raw control characters (newline, tab, CR) inside JSON string values.
        // The LLM occasionally emits literal newlines inside a quoted value, breaking the parser.
        private static string SanitizeJson(string text)
        {
            var sb      = new System.Text.StringBuilder(text.Length);
            bool inStr  = false;
            bool escape = false;

            foreach (char c in text)
            {
                if (escape) { sb.Append(c); escape = false; continue; }

                if (c == '\\' && inStr) { escape = true; sb.Append(c); continue; }

                if (c == '"') { inStr = !inStr; sb.Append(c); continue; }

                if (inStr)
                {
                    switch (c)
                    {
                        case '\n': sb.Append("\\n");  break;
                        case '\r': sb.Append("\\r");  break;
                        case '\t': sb.Append("\\t");  break;
                        default:   sb.Append(c);      break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
