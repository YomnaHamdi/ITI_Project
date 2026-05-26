using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    // ── Inbound ──────────────────────────────────────────────────────────────────
    public record GenerateStoryRequest(
        string ChildName,
        string Character,
        string Theme
    );

    // ── Outbound ─────────────────────────────────────────────────────────────────
    public record GenerateStoryResponse(
        Guid Id,
        string Title,
        List<StoryPageResponse> Pages
    );

    public record StoryPageResponse(
        int PageNumber,
        string Sentence,
        string ImageUrl
    );

    // ── Internal AI contract ─────────────────────────────────────────────────────
    // Represents the JSON structure we expect Qwen3 to return
    public record AiStoryOutput(
        string Title,
        List<AiStoryPage> Pages
    );

    public record AiStoryPage(
        int PageNumber,
        string Sentence,
        string ImagePrompt
    );

}
