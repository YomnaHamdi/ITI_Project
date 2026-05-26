using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Agent
{
    /// <summary>
    /// StoryAgent orchestrates the full pipeline using Microsoft.Extensions.AI:
    ///
    ///  Step 1 → IChatClient (Qwen2.5:1.5b via Ollama)
    ///           → Arabic story JSON  (sentences in Arabic, imagePrompts in English)
    ///
    ///  Step 2 → IImageGenerationService (ComfyUI workflow.json)
    ///           → one PNG per page
    ///
    ///  Step 3 → IStoryRepository (SQL Server via EF Core)
    ///           → persist everything
    ///
    ///  Step 4 → return GenerateStoryResponse to the controller
    /// </summary>
    public sealed class StoryAgent(
        IChatClient chatClient,
        IStoryGeneratorService storyGenerator,
        IImageGenerationService imageGenerator,
        IStoryRepository storyRepository,
        ILogger<StoryAgent> logger)
    {
        public async Task<GenerateStoryResponse> RunAsync(GenerateStoryRequest request)
        {
            logger.LogInformation(
                "=== StoryAgent START | child:{Child} char:{Char} theme:{Theme} ===",
                request.ChildName, request.Character, request.Theme);

            // ── STEP 1: Generate Arabic story text via Qwen2.5:1.5b ───────────────
            logger.LogInformation("[Agent] Step 1 — Calling Qwen2.5:1.5b for Arabic story...");
            var aiOutput = await storyGenerator.GenerateAsync(
                request.ChildName, request.Character, request.Theme);

            logger.LogInformation("[Agent] Step 1 DONE — title: \"{Title}\"", aiOutput.Title);

            // ── STEP 2: Build domain entity ────────────────────────────────────────
            var story = new Story
            {
                ChildName = request.ChildName,
                Character = request.Character,
                Theme = request.Theme,
                Title = aiOutput.Title
            };

            // ── STEP 3: Generate images via ComfyUI (sequential to avoid overload) ─
            logger.LogInformation("[Agent] Step 2 — Sending image prompts to ComfyUI...");

            foreach (var page in aiOutput.Pages.OrderBy(p => p.PageNumber))
            {
                logger.LogInformation(
                    "[Agent] Generating image {Page}/3 | prompt: {Prompt}",
                    page.PageNumber, page.ImagePrompt);

                var fileName = $"{story.Id}_page{page.PageNumber}.png";
                var imageUrl = await imageGenerator.GenerateImageAsync(page.ImagePrompt, fileName);

                story.Pages.Add(new StoryPage
                {
                    StoryId = story.Id,
                    PageNumber = page.PageNumber,
                    Sentence = page.Sentence,     // Arabic sentence for display
                    ImagePrompt = page.ImagePrompt,  // English prompt (for audit/debug)
                    ImagePath = imageUrl           // returned by ComfyUI
                });

                logger.LogInformation("[Agent] Page {Page} image saved: {Url}", page.PageNumber, imageUrl);
            }

            // ── STEP 4: Persist to SQL Server ─────────────────────────────────────
            logger.LogInformation("[Agent] Step 3 — Saving story to database...");
            await storyRepository.SaveAsync(story);

            // ── STEP 5: Return response ────────────────────────────────────────────
            var response = new GenerateStoryResponse(
                story.Id,
                story.Title,
                story.Pages
                     .OrderBy(p => p.PageNumber)
                     .Select(p => new StoryPageResponse(p.PageNumber, p.Sentence, p.ImagePath))
                     .ToList());

            logger.LogInformation("=== StoryAgent COMPLETE | storyId: {Id} ===", story.Id);
            return response;
        }
    }
}
