using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SkiaSharp;

namespace Infrastructure.Rag
{
    public class PdfDocumentProcessor(
        IOptions<RagSettings> settings,
        IOcrService ocrService,
        IVisionDescriptionService visionService,
        ILogger<PdfDocumentProcessor> logger) : IDocumentProcessor
    {
        private readonly RagSettings _cfg = settings.Value;

        public bool CanProcess(string extension) =>
            extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        public async Task<List<DocumentChunk>> ProcessAsync(string filePath, DocumentMetadata meta)
        {
            var chunks    = new List<DocumentChunk>();
            var pdfBytes  = await File.ReadAllBytesAsync(filePath);
            var pageCount = Conversion.GetPageCount(pdfBytes, password: null);

            logger.LogInformation("[PDF-RAG] Processing {File} — {Pages} pages", filePath, pageCount);

            for (int i = 0; i < pageCount; i++)
            {
                var pageNum  = i + 1;
                var tempPath = Path.Combine(Path.GetTempPath(), $"rag_page_{Guid.NewGuid()}.png");
                try
                {
                    using var bitmap = Conversion.ToImage(pdfBytes, page: i, options: new(Dpi: 150));
                    using var image  = SKImage.FromBitmap(bitmap);
                    using var data   = image.Encode(SKEncodedImageFormat.Png, 90);
                    await File.WriteAllBytesAsync(tempPath, data.ToArray());

                    var text = await ocrService.ExtractArabicTextAsync(tempPath);

                    if (string.IsNullOrWhiteSpace(text) || text.Length < 30)
                    {
                        logger.LogInformation("[PDF-RAG] Page {N} sparse OCR — using vision fallback", pageNum);
                        text = await visionService.DescribeArabicEducationalImageAsync(tempPath);
                    }

                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var pageChunks = ArabicTextChunker.Split(text, _cfg.ChunkSize, _cfg.ChunkOverlap);
                    for (int j = 0; j < pageChunks.Count; j++)
                    {
                        chunks.Add(new DocumentChunk(
                            pageChunks[j], pageNum,
                            $"{Path.GetFileNameWithoutExtension(filePath)}_p{pageNum}_c{j + 1}"));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[PDF-RAG] Failed to process page {N}", pageNum);
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            logger.LogInformation("[PDF-RAG] Extracted {Count} chunks from {File}", chunks.Count, meta.FileName);
            return chunks;
        }
    }
}
