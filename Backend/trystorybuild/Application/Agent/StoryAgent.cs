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
     IStoryGeneratorService storyGenerator,
     IJudgeService judgeService,
     IImageGenerationService imageGenerator,
     IStoryRepository storyRepository,
     ILogger<StoryAgent> logger)
    {
        public async Task<GenerateStoryResponse> RunAsync(GenerateStoryRequest request)
        {
            logger.LogInformation(
                "=== StoryAgent START | child:{Child} char:{Char} theme:{Theme} ===",
                request.ChildName, request.Character, request.Theme);

            // Step 1: Generate Arabic story via Qwen2.5:1.5b
            logger.LogInformation("[StoryAgent] Step 1 — Calling Qwen2.5 for Arabic story...");
            var aiOutput = await storyGenerator.GenerateAsync(
                request.ChildName, request.Character, request.Theme);

            // Step 2: Judge validates child safety
            logger.LogInformation("[StoryAgent] Step 2 — Running JudgeAgent...");
            var sentences = aiOutput.Pages.Select(p => p.Sentence).ToList();
            var imagePrompts = aiOutput.Pages.Select(p => p.ImagePrompt).ToList();
            var judgeResult = await judgeService.ValidateAsync(aiOutput.Title, sentences, imagePrompts);
            logger.LogInformation("[StoryAgent] Judge: approved={Ok} reason={Reason}",
                judgeResult.IsApproved, judgeResult.Reason);

            // Step 3: Build story entity
            var story = new Story
            {
                ChildName = request.ChildName,
                Character = request.Character,
                Theme = request.Theme,
                Title = aiOutput.Title,
                IsApproved = judgeResult.IsApproved
            };

            // Step 4: Generate images via ComfyUI (sequential — one GPU)
            logger.LogInformation("[StoryAgent] Step 3 — Generating {Count} images via ComfyUI...",
                aiOutput.Pages.Count);

            foreach (var page in aiOutput.Pages.OrderBy(p => p.PageNumber))
            {
                var fileName = $"{story.Id}_page{page.PageNumber}.png";
                var imageUrl = await imageGenerator.GenerateImageAsync(page.ImagePrompt, fileName);

                story.Pages.Add(new StoryPage
                {
                    StoryId = story.Id,
                    PageNumber = page.PageNumber,
                    Sentence = page.Sentence,
                    ImagePrompt = page.ImagePrompt,
                    ImagePath = imageUrl,
                    IsUnlocked = true  // AI stories have no writing gate
                });

                logger.LogInformation("[StoryAgent] Page {N} image saved: {Url}", page.PageNumber, imageUrl);
            }

            // Step 5: Persist
            await storyRepository.SaveAsync(story);
            logger.LogInformation("=== StoryAgent COMPLETE | storyId:{Id} ===", story.Id);

            return MapToResponse(story);
        }

        public static GenerateStoryResponse MapToResponse(Story story) =>
            new(story.Id,
                story.Title,
                story.IsApproved,
                story.Pages
                     .OrderBy(p => p.PageNumber)
                     .Select(p => new StoryPageDto(
                         p.Id,
                         p.PageNumber,
                         p.Sentence,
                         p.ImagePath,
                         p.IsUnlocked
                        ))
                     .ToList());
    }
}
