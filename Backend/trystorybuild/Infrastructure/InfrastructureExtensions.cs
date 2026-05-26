using Application.Interfaces;
using Infrastructure.AI;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ── SQL Server ────────────────────────────────────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            // ── Ollama / Qwen2.5:1.5b ─────────────────────────────────────────────
            // OllamaSharp.OllamaApiClient implements IChatClient (Microsoft.Extensions.AI).
            // Using AddSingleton so the HTTP connection pool is reused.
            var ollamaEndpoint = new Uri(configuration["Ollama:Endpoint"] ?? "http://localhost:11434");
            var ollamaModel = configuration["Ollama:Model"] ?? "qwen2.5:1.5b";

            services.AddSingleton<IChatClient>(sp =>
            {
                var ollamaClient = new OllamaApiClient(ollamaEndpoint, ollamaModel);
                return new ChatClientBuilder(ollamaClient)
                    .UseLogging(sp.GetRequiredService<ILoggerFactory>())
                    .Build();
            });

            // ── ComfyUI (workflow.json image generation) ──────────────────────────
            services.Configure<ComfyUiSettings>(configuration.GetSection("ComfyUI"));

            services.AddHttpClient<ComfyUiImageService>(client =>
            {
                // ComfyUI generation can take several minutes on low-end hardware
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            // ── Interface bindings ────────────────────────────────────────────────
            services.AddScoped<IStoryGeneratorService, QwenStoryGeneratorService>();
            services.AddScoped<IImageGenerationService, ComfyUiImageService>();
            services.AddScoped<IStoryRepository, StoryRepository>();

            return services;
        }
    }

}
