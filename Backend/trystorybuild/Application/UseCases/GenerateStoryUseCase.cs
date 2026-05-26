using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.UseCases
{
    public class GenerateStoryUseCase(
    IStoryGeneratorService storyGenerator,
    IImageGenerationService imageGenerator,
    IStoryRepository storyRepository,
    ILogger<GenerateStoryUseCase> logger)
    {
        public async Task<GenerateStoryResponse> ExecuteAsync(GenerateStoryRequest request)
        {
            // 1. Ask Qwen3 to write a 3-page Arabic story
            logger.LogInformation("Calling LLM for story generation...");
            var aiOutput = await storyGenerator.GenerateAsync(
                request.ChildName,
                request.Character,
                request.Theme);

            // 2. Build the domain entity
            var story = new Story
            {
                ChildName = request.ChildName,
                Character = request.Character,
                Theme = request.Theme,
                Title = aiOutput.Title
            };

            // 3. Generate one image per page in parallel
            logger.LogInformation("Generating {Count} images in parallel...", aiOutput.Pages.Count);

            var imageResults = await Task.WhenAll(
                aiOutput.Pages.Select(async aiPage =>
                {
                    var fileName = $"{story.Id}_page{aiPage.PageNumber}.png";
                    // imageUrl is the URL path returned by the Python server
                    // e.g. "/images/guid_page1.png"
                    var imageUrl = await imageGenerator.GenerateImageAsync(aiPage.ImagePrompt, fileName);
                    return (aiPage, fileName, imageUrl);
                }));

            // 4. Attach pages to the story entity
            foreach (var (aiPage, fileName, imageUrl) in imageResults.OrderBy(r => r.aiPage.PageNumber))
            {
                story.Pages.Add(new StoryPage
                {
                    StoryId = story.Id,
                    PageNumber = aiPage.PageNumber,
                    Sentence = aiPage.Sentence,
                    ImagePrompt = aiPage.ImagePrompt,
                    ImagePath = imageUrl   // store the URL path as returned by Python
                });
            }

            // 5. Persist to SQL Server
            await storyRepository.SaveAsync(story);
            logger.LogInformation("Story saved with Id: {Id}", story.Id);

            // 6. Build and return the API response
            // imageUrl is already the correct path — no re-wrapping needed
            var pageResponses = story.Pages
                .OrderBy(p => p.PageNumber)
                .Select(p => new StoryPageResponse(p.PageNumber, p.Sentence, p.ImagePath))
                .ToList();

            return new GenerateStoryResponse(story.Id, story.Title, pageResponses);
        }
    }
}
