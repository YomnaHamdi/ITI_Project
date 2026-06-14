using Application.Agent;
using Application.Interfaces;
using Infrastructure.AI;
using Infrastructure.Auth;
using Infrastructure.Data;
using Infrastructure.Pdf;
using Infrastructure.Rag;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OllamaSharp;

namespace Infrastructure
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            string contentRootPath = "")
        {
            // ── SQL Server ─────────────────────────────────────────────────────────
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // ── Ollama / Qwen2.5:1.5b ─────────────────────────────────────────────
            var ollamaEndpoint = new Uri(configuration["Ollama:Endpoint"] ?? "http://localhost:11434");
            var ollamaModel    = configuration["Ollama:Model"] ?? "qwen2.5:1.5b";
            services.AddHttpClient("Ollama", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            // ── Gemini API ────────────────────────────────────────────────────────
            services.AddHttpClient("Gemini", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddSingleton<IChatClient>(sp =>
            {
                var client = new OllamaApiClient(ollamaEndpoint, ollamaModel);
                return new ChatClientBuilder(client)
                    .UseLogging(sp.GetRequiredService<ILoggerFactory>())
                    .Build();
            });

            // ── ComfyUI ───────────────────────────────────────────────────────────
            services.Configure<ComfyUiSettings>(configuration.GetSection("ComfyUI"));
            services.AddHttpClient<ComfyUiImageService>(client =>
                client.Timeout = TimeSpan.FromMinutes(5));

            // ── Tesseract OCR ─────────────────────────────────────────────────────
            services.Configure<TesseractSettings>(configuration.GetSection("Tesseract"));
            services.AddScoped<TesseractOcrService>();

            // ── PDF Import ────────────────────────────────────────────────────────
            services.Configure<PdfImportSettings>(configuration.GetSection("PdfImport"));

            // ── AI Services ───────────────────────────────────────────────────────
            services.AddScoped<IStoryGeneratorService, QwenStoryGeneratorService>();
            services.AddScoped<IExamGeneratorService, GeminiExamGeneratorService>();
            services.AddScoped<IJudgeService, QwenJudgeService>();
            services.AddScoped<IImageGenerationService, ComfyUiImageService>();
            services.AddScoped<IOcrService, TesseractOcrService>();
            services.AddScoped<ITextSimilarityService, ArabicSimilarityService>();
            services.AddScoped<IAiTextCleanupService, QwenOcrCleanupService>();

            // ── PDF Import ────────────────────────────────────────────────────────
            services.AddScoped<IPdfPageRenderer, PdfPageRenderer>();
            services.AddScoped<IPdfImportService, PdfImportService>();

            // ── Auth ──────────────────────────────────────────────────────────────
            services.AddScoped<IAuthService,       AuthService>();
            services.AddScoped<IUserRepository,    UserRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();

            // ── Repositories ──────────────────────────────────────────────────────
            services.AddScoped<IStoryRepository, StoryRepository>();
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<IExamRepository, ExamRepository>();
            services.AddScoped<IStudentProgressRepository, StudentProgressRepository>();
            services.AddScoped<IWritingAttemptRepository, WritingAttemptRepository>();
            services.AddScoped<IPlacementRepository, PlacementRepository>();

            // ── New Repositories (Groups, Assignments, RAG Chunks, Word Config) ───
            services.AddScoped<ILevelWordConfigRepository, LevelWordConfigRepository>();
            services.AddScoped<IRagPageChunkRepository, RagPageChunkRepository>();
            services.AddScoped<IStudentGroupRepository, StudentGroupRepository>();
            services.AddScoped<ILessonAssignmentRepository, LessonAssignmentRepository>();

            // ── Dashboard Service ──────────────────────────────────────────────────
            services.AddScoped<IDashboardService, DashboardService>();

            // ── RAG ───────────────────────────────────────────────────────────────
            services.Configure<RagSettings>(configuration.GetSection("Rag"));
            services.AddHttpClient<OllamaEmbeddingService>();
            services.AddHttpClient<ChromaVectorStoreService>();
            services.AddHttpClient<OllamaVisionDescriptionService>();

            services.AddScoped<IEmbeddingService,         OllamaEmbeddingService>();
            services.AddScoped<IVectorStoreService,       ChromaVectorStoreService>();
            services.AddScoped<IVisionDescriptionService, OllamaVisionDescriptionService>();
            services.AddScoped<IRagIngestionService,      RagIngestionService>();
            services.AddScoped<IRagQueryService,          RagQueryService>();

            // Document processors — all registered, dispatcher picks by extension
            services.AddScoped<IDocumentProcessor, PdfDocumentProcessor>();
            services.AddScoped<IDocumentProcessor, PptxDocumentProcessor>();
            services.AddScoped<IDocumentProcessor, ImageDocumentProcessor>();

            // Educational PDF library (fixed-structure per-page extraction)
            var rootPath = string.IsNullOrEmpty(contentRootPath) ? Directory.GetCurrentDirectory() : contentRootPath;
            services.AddScoped<IEducationalPdfService>(sp =>
                new EducationalPdfService(
                    sp.GetRequiredService<AppDbContext>(),
                    sp.GetRequiredService<IEmbeddingService>(),
                    sp.GetRequiredService<IVectorStoreService>(),
                    rootPath,
                    sp.GetRequiredService<ILogger<EducationalPdfService>>()));

            // Per-page RAG ingestion (Gemini Vision extracts sentence from each page image)
            services.AddScoped<IEducationalPdfIngestionService, EducationalPdfIngestionService>();

            // Lesson generation agent (Gemini + ComfyUI)
            services.AddScoped<LessonGenerationAgent>();

            return services;
        }
    }
}
