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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            // ── HTTP clients ───────────────────────────────────────────────────────
            services.AddHttpClient("Gemini",     client => client.Timeout = TimeSpan.FromSeconds(120));
            services.AddHttpClient("Cloudflare", client => client.Timeout = TimeSpan.FromSeconds(120));

            // ── AI Services (Gemini + Cloudflare — fully cloud, no local deps) ─────
            services.AddScoped<IStoryGeneratorService,  GeminiStoryGeneratorService>();
            services.AddScoped<IExamGeneratorService,   GeminiExamGeneratorService>();
            services.AddScoped<IJudgeService,           GeminiJudgeService>();
            services.AddScoped<IImageGenerationService, CloudflareImageService>();
            services.AddScoped<IOcrService,             GeminiOcrService>();
            services.AddScoped<ITextSimilarityService,  ArabicSimilarityService>();
            services.AddScoped<IAiTextCleanupService,   GeminiTextCleanupService>();

            // ── PDF Import ────────────────────────────────────────────────────────
            services.Configure<PdfImportSettings>(configuration.GetSection("PdfImport"));
            services.AddScoped<IPdfPageRenderer, PdfPageRenderer>();
            services.AddScoped<IPdfImportService, PdfImportService>();

            // ── Auth ──────────────────────────────────────────────────────────────
            services.AddScoped<IAuthService,       AuthService>();
            services.AddScoped<IUserRepository,    UserRepository>();
            services.AddScoped<IStudentRepository, StudentRepository>();

            // ── Repositories ──────────────────────────────────────────────────────
            services.AddScoped<IStoryRepository,             StoryRepository>();
            services.AddScoped<ILessonRepository,            LessonRepository>();
            services.AddScoped<IExamRepository,              ExamRepository>();
            services.AddScoped<IStudentProgressRepository,   StudentProgressRepository>();
            services.AddScoped<IWritingAttemptRepository,    WritingAttemptRepository>();
            services.AddScoped<IPlacementRepository,         PlacementRepository>();
            services.AddScoped<ILevelWordConfigRepository,   LevelWordConfigRepository>();
            services.AddScoped<IRagPageChunkRepository,      RagPageChunkRepository>();
            services.AddScoped<IStudentGroupRepository,      StudentGroupRepository>();
            services.AddScoped<ILessonAssignmentRepository,  LessonAssignmentRepository>();

            // ── Dashboard Service ──────────────────────────────────────────────────
            services.AddScoped<IDashboardService, DashboardService>();

            // ── RAG (Gemini embeddings + Chroma Cloud) ─────────────────────────────
            services.Configure<RagSettings>(configuration.GetSection("Rag"));
            services.AddHttpClient<GeminiEmbeddingService>();
            services.AddHttpClient<ChromaVectorStoreService>();
            services.AddHttpClient<GeminiVisionDescriptionService>();

            services.AddScoped<IEmbeddingService,         GeminiEmbeddingService>();
            services.AddScoped<IVectorStoreService,       ChromaVectorStoreService>();
            services.AddScoped<IVisionDescriptionService, GeminiVisionDescriptionService>();
            services.AddScoped<IRagIngestionService,      RagIngestionService>();
            services.AddScoped<IRagQueryService,          RagQueryService>();

            // Document processors — all registered, dispatcher picks by extension
            services.AddScoped<IDocumentProcessor, PdfDocumentProcessor>();
            services.AddScoped<IDocumentProcessor, PptxDocumentProcessor>();
            services.AddScoped<IDocumentProcessor, ImageDocumentProcessor>();

            // Educational PDF library
            var rootPath = string.IsNullOrEmpty(contentRootPath) ? Directory.GetCurrentDirectory() : contentRootPath;
            services.AddScoped<IEducationalPdfService>(sp =>
                new EducationalPdfService(
                    sp.GetRequiredService<AppDbContext>(),
                    sp.GetRequiredService<IEmbeddingService>(),
                    sp.GetRequiredService<IVectorStoreService>(),
                    rootPath,
                    sp.GetRequiredService<ILogger<EducationalPdfService>>()));

            services.AddScoped<IEducationalPdfIngestionService, EducationalPdfIngestionService>();

            // Lesson generation agent (Gemini + Cloudflare)
            services.AddScoped<LessonGenerationAgent>();

            return services;
        }
    }
}
