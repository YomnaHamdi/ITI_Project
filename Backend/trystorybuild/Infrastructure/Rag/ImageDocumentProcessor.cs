using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Rag
{
    public class ImageDocumentProcessor(
        IOptions<RagSettings> settings,
        IVisionDescriptionService visionService,
        ILogger<ImageDocumentProcessor> logger) : IDocumentProcessor
    {
        private readonly RagSettings _cfg = settings.Value;

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif"
        };

        public bool CanProcess(string extension) => ImageExtensions.Contains(extension);

        public async Task<List<DocumentChunk>> ProcessAsync(string filePath, DocumentMetadata meta)
        {
            logger.LogInformation("[Image-RAG] Describing {File}", filePath);
            var description = await visionService.DescribeArabicEducationalImageAsync(filePath);

            if (string.IsNullOrWhiteSpace(description))
            {
                logger.LogWarning("[Image-RAG] Empty description for {File}", filePath);
                return new();
            }

            var chunks = ArabicTextChunker.Split(description, _cfg.ChunkSize, _cfg.ChunkOverlap);
            return chunks.Select((text, idx) => new DocumentChunk(
                text, 1,
                $"{Path.GetFileNameWithoutExtension(filePath)}_c{idx + 1}"))
            .ToList();
        }
    }
}
