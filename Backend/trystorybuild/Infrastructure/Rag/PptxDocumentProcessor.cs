using Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Infrastructure.Rag
{
    public class PptxDocumentProcessor(
        IOptions<RagSettings> settings,
        IVisionDescriptionService visionService,
        ILogger<PptxDocumentProcessor> logger) : IDocumentProcessor
    {
        private readonly RagSettings _cfg = settings.Value;

        public bool CanProcess(string extension) =>
            extension.Equals(".pptx", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ppt",  StringComparison.OrdinalIgnoreCase);

        public async Task<List<DocumentChunk>> ProcessAsync(string filePath, DocumentMetadata meta)
        {
            var chunks = new List<DocumentChunk>();
            logger.LogInformation("[PPTX-RAG] Processing {File}", filePath);

            using var presentation  = PresentationDocument.Open(filePath, isEditable: false);
            var presentationPart    = presentation.PresentationPart
                ?? throw new InvalidOperationException("Invalid PPTX file.");

            var slideIds = presentationPart.Presentation.SlideIdList?
                .OfType<SlideId>().ToList() ?? new();

            for (int i = 0; i < slideIds.Count; i++)
            {
                var slideNum = i + 1;
                var relId    = slideIds[i].RelationshipId?.Value;
                if (relId is null) continue;

                var slidePart = (SlidePart)presentationPart.GetPartById(relId);
                var slideText = ExtractSlideText(slidePart);
                var notes     = ExtractNotes(slidePart);
                if (!string.IsNullOrWhiteSpace(notes)) slideText += " " + notes;

                var imageDescs = await ExtractAndDescribeImagesAsync(slidePart, slideNum);
                foreach (var desc in imageDescs) slideText += " " + desc;

                if (string.IsNullOrWhiteSpace(slideText.Trim())) continue;

                var slideChunks = ArabicTextChunker.Split(slideText.Trim(), _cfg.ChunkSize, _cfg.ChunkOverlap);
                for (int j = 0; j < slideChunks.Count; j++)
                {
                    chunks.Add(new DocumentChunk(
                        slideChunks[j], slideNum,
                        $"{Path.GetFileNameWithoutExtension(filePath)}_s{slideNum}_c{j + 1}"));
                }
            }

            logger.LogInformation("[PPTX-RAG] Extracted {Count} chunks from {File}", chunks.Count, meta.FileName);
            return chunks;
        }

        private static string ExtractSlideText(SlidePart slidePart)
        {
            var sb = new StringBuilder();
            foreach (var t in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                var text = t.Text?.Trim();
                if (!string.IsNullOrEmpty(text)) sb.Append(text).Append(' ');
            }
            return sb.ToString();
        }

        private static string ExtractNotes(SlidePart slidePart)
        {
            if (slidePart.NotesSlidePart is null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var t in slidePart.NotesSlidePart.NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
            {
                var text = t.Text?.Trim();
                if (!string.IsNullOrEmpty(text)) sb.Append(text).Append(' ');
            }
            return sb.ToString();
        }

        private async Task<List<string>> ExtractAndDescribeImagesAsync(SlidePart slidePart, int slideNum)
        {
            var descriptions = new List<string>();
            foreach (var imagePart in slidePart.ImageParts)
            {
                try
                {
                    var ext      = Path.GetExtension(imagePart.Uri.ToString());
                    var tempPath = Path.Combine(Path.GetTempPath(), $"rag_slide{slideNum}_{Guid.NewGuid()}{ext}");
                    using (var stream = imagePart.GetStream())
                    using (var file   = File.Create(tempPath))
                        await stream.CopyToAsync(file);
                    var desc = await visionService.DescribeArabicEducationalImageAsync(tempPath);
                    if (!string.IsNullOrWhiteSpace(desc)) descriptions.Add(desc);
                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[PPTX-RAG] Image extraction failed on slide {N}", slideNum);
                }
            }
            return descriptions;
        }
    }
}
