using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    /// <summary>Calls the local Qwen3 LLM and returns structured story JSON.</summary>
    public interface IStoryGeneratorService
    {
        Task<AiStoryOutput> GenerateAsync(string childName, string character, string theme);
    }

    /// <summary>Calls Z-Image Turbo, saves the PNG to disk, returns the saved file path.</summary>
    public interface IImageGenerationService
    {
        Task<string> GenerateImageAsync(string prompt, string fileName);
    }

    /// <summary>Persists and retrieves Story aggregates.</summary>
    public interface IStoryRepository
    {
        Task SaveAsync(Story story);
        Task<Story?> GetByIdAsync(Guid id);
    }

}
