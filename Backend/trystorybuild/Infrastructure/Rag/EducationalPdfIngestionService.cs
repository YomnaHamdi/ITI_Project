using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFtoImage;
using SkiaSharp;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Rag
{
    public class EducationalPdfIngestionService(
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore,
        IRagPageChunkRepository chunkRepository,
        ILevelWordConfigRepository wordConfigRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IOptions<RagSettings> settings,
        ILogger<EducationalPdfIngestionService> logger) : IEducationalPdfIngestionService
    {
        private readonly RagSettings _cfg = settings.Value;
        private const int MaxContentPages = 3;

        public async Task<IngestDocumentResponse> IngestAsync(
            Stream pdfStream,
            string fileName,
            int level,
            string letter,
            string letterName,
            CancellationToken ct = default)
        {
            var docId = Guid.NewGuid();
            var uploadsDir = _cfg.DocumentsDirectory;
            Directory.CreateDirectory(uploadsDir);

            var pdfPath = Path.Combine(uploadsDir, $"{docId}.pdf");
            await using (var file = File.Create(pdfPath))
                await pdfStream.CopyToAsync(file, ct);

            logger.LogInformation("[EduPDF] Saved → {Path}", pdfPath);

            var pdfBytes = await File.ReadAllBytesAsync(pdfPath, ct);
            var pageCount = Conversion.GetPageCount(pdfBytes, password: null);

            logger.LogInformation("[EduPDF] {File} has {Pages} pages", fileName, pageCount);

            if (pageCount < 3)
            {
                logger.LogWarning("[EduPDF] Too few pages ({Count}), skipping.", pageCount);
                return new IngestDocumentResponse(docId, fileName, "pdf", 0, "الكتاب لا يحتوي على صفحات كافية (الحد الأدنى 3).");
            }

            await vectorStore.EnsureCollectionAsync();
            await vectorStore.DeleteBySourceAsync(fileName);
            await chunkRepository.DeleteBySourceFileAsync(fileName);

            var chunkIds   = new List<string>();
            var embeddings = new List<float[]>();
            var texts      = new List<string>();
            var metadatas  = new List<Dictionary<string, string>>();
            var savedChunks = 0;
            var wordCounts  = new List<int>();

            // Pages to process: skip first (cover) and last page
            var startPage  = 1;            // 0-indexed → page index 1 = page 2
            var endPage    = pageCount - 2; // exclusive of last page
            var processed  = 0;

            for (int i = startPage; i <= endPage && processed < MaxContentPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                var pageNum = i + 1;
                var tempPath = Path.Combine(Path.GetTempPath(), $"edu_page_{Guid.NewGuid()}.png");

                try
                {
                    // Render page to image
                    using var bitmap = Conversion.ToImage(pdfBytes, page: i, options: new(Dpi: 150));
                    using var image  = SKImage.FromBitmap(bitmap);
                    using var data   = image.Encode(SKEncodedImageFormat.Png, 90);
                    await File.WriteAllBytesAsync(tempPath, data.ToArray(), ct);

                    // Gemini Vision extracts the sentence
                    var sentence = await ExtractSentenceWithGeminiAsync(tempPath, letter, ct);
                    if (string.IsNullOrWhiteSpace(sentence))
                    {
                        logger.LogWarning("[EduPDF] Page {N} — empty sentence, skipping", pageNum);
                        continue;
                    }

                    var wordCount = sentence.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                    wordCounts.Add(wordCount);

                    // Save page image permanently
                    var imagesDir = Path.Combine("wwwroot", "images", "rag", docId.ToString());
                    Directory.CreateDirectory(imagesDir);
                    var imageName = $"page_{pageNum}.png";
                    var imageSavePath = Path.Combine(imagesDir, imageName);
                    File.Copy(tempPath, imageSavePath, overwrite: true);
                    var imageWebPath = $"/images/rag/{docId}/{imageName}";

                    // Embed the sentence
                    var vector = await embeddingService.GetEmbeddingAsync(sentence);
                    var chunkId = $"{docId}_p{pageNum}";

                    chunkIds.Add(chunkId);
                    embeddings.Add(vector);
                    texts.Add(sentence);
                    metadatas.Add(new Dictionary<string, string>
                    {
                        ["source_file"] = fileName,
                        ["page_number"] = pageNum.ToString(),
                        ["letter"]      = letter,
                        ["level"]       = level.ToString(),
                        ["word_count"]  = wordCount.ToString(),
                        ["image_path"]  = imageWebPath
                    });

                    // Save SQL record
                    await chunkRepository.SaveAsync(new RagPageChunk
                    {
                        SourceFile   = fileName,
                        PageNumber   = pageNum,
                        Sentence     = sentence,
                        WordCount    = wordCount,
                        ImagePath    = imageWebPath,
                        Level        = level,
                        Letter       = letter,
                        LetterName   = letterName,
                        ChromaChunkId = chunkId
                    });

                    savedChunks++;
                    processed++;
                    logger.LogInformation("[EduPDF] Page {N}: '{Sentence}' ({Words} words)", pageNum, sentence, wordCount);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[EduPDF] Failed to process page {N}", pageNum);
                }
                finally
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
            }

            if (chunkIds.Count > 0)
                await vectorStore.AddChunksAsync(chunkIds, embeddings, texts, metadatas);

            // Update LevelWordConfig with the average word count from this PDF
            if (wordCounts.Count > 0)
            {
                var avgWords = (int)Math.Round(wordCounts.Average());
                var exampleSentence = texts.Count > 0 ? texts[0] : string.Empty;
                await wordConfigRepository.UpsertAsync(level, avgWords, exampleSentence);
                logger.LogInformation("[EduPDF] Updated LevelWordConfig: level={L}, avgWords={W}", level, avgWords);
            }

            return new IngestDocumentResponse(
                docId, fileName, "pdf", savedChunks,
                $"تم معالجة {savedChunks} صفحة من كتاب '{letter}' المستوى {level} بنجاح.");
        }

        private async Task<string> ExtractSentenceWithGeminiAsync(string imagePath, string letter, CancellationToken ct)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return string.Empty;

            try
            {
                var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
                var base64 = Convert.ToBase64String(imageBytes);

                var prompt = $"""
                    You are an Arabic text extractor for children's educational books.
                    This page is from a book teaching the Arabic letter '{letter}'.
                    Extract ONLY the Arabic sentence or phrase written on this page.
                    Return ONLY the Arabic text — no explanation, no punctuation you added.
                    If there is no Arabic text visible, return an empty string.
                    """;

                var body = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new { inline_data = new { mime_type = "image/png", data = base64 } }
                            }
                        }
                    }
                };

                var model  = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
                var url    = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var client = httpClientFactory.CreateClient("Gemini");
                var resp   = await client.PostAsJsonAsync(url, body, ct);
                resp.EnsureSuccessStatusCode();

                var raw = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(raw);
                var sentence = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                return sentence.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[EduPDF] Gemini Vision failed for {Path}", imagePath);
                return string.Empty;
            }
        }
    }
}
