using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Rag
{
    public class RagIngestionService(
        IEnumerable<IDocumentProcessor> processors,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore,
        AppDbContext db,
        IOptions<RagSettings> settings,
        ILogger<RagIngestionService> logger) : IRagIngestionService
    {
        private readonly RagSettings _cfg = settings.Value;

        public async Task<IngestDocumentResponse> IngestAsync(
            Stream fileStream, string fileName, IngestDocumentRequest request,
            CancellationToken ct = default)
        {
            var ext       = Path.GetExtension(fileName).ToLowerInvariant();
            var processor = processors.FirstOrDefault(p => p.CanProcess(ext))
                ?? throw new InvalidOperationException($"No processor for '{ext}' files.");

            Directory.CreateDirectory(_cfg.DocumentsDirectory);
            var docId    = Guid.NewGuid();
            var filePath = Path.Combine(_cfg.DocumentsDirectory, $"{docId}{ext}");

            await using (var file = File.Create(filePath))
                await fileStream.CopyToAsync(file, ct);

            logger.LogInformation("[RAG] Saved file → {Path}", filePath);

            var meta   = new DocumentMetadata(fileName, request.Letter, request.Level, request.Tags);
            var chunks = await processor.ProcessAsync(filePath, meta);

            if (chunks.Count == 0)
                throw new InvalidOperationException("Document produced no text chunks.");

            logger.LogInformation("[RAG] {File} → {Count} chunks", fileName, chunks.Count);

            await vectorStore.EnsureCollectionAsync();
            await vectorStore.DeleteBySourceAsync(fileName);

            var ids        = new List<string>();
            var embeddings = new List<float[]>();
            var texts      = new List<string>();
            var metadatas  = new List<Dictionary<string, string>>();

            for (int i = 0; i < chunks.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                logger.LogInformation("[RAG] Embedding chunk {I}/{Total}", i + 1, chunks.Count);
                var vector = await embeddingService.GetEmbeddingAsync(chunks[i].Text);
                ids.Add(chunks[i].ChunkId);
                embeddings.Add(vector);
                texts.Add(chunks[i].Text);
                metadatas.Add(new Dictionary<string, string>
                {
                    ["source_file"]   = fileName,
                    ["page_number"]   = chunks[i].PageNumber.ToString(),
                    ["letter"]        = request.Letter ?? "",
                    ["level"]         = request.Level?.ToString() ?? "",
                    ["tags"]          = request.Tags ?? "",
                    ["document_type"] = ext.TrimStart('.')
                });
            }

            await vectorStore.AddChunksAsync(ids, embeddings, texts, metadatas);

            var doc = new KnowledgeDocument
            {
                Id           = docId,
                FileName     = fileName,
                DocumentType = ext.TrimStart('.'),
                Letter       = request.Letter,
                Level        = request.Level,
                Tags         = request.Tags,
                ChunkCount   = chunks.Count,
                FilePath     = filePath
            };

            db.KnowledgeDocuments.Add(doc);
            await db.SaveChangesAsync(ct);

            return new IngestDocumentResponse(
                doc.Id, doc.FileName, doc.DocumentType, doc.ChunkCount,
                $"تم استيراد {chunks.Count} مقطع بنجاح من {fileName}");
        }

        public async Task DeleteAsync(Guid documentId)
        {
            var doc = await db.KnowledgeDocuments.FindAsync(documentId)
                ?? throw new InvalidOperationException($"Document {documentId} not found.");
            await vectorStore.DeleteBySourceAsync(doc.FileName);
            if (File.Exists(doc.FilePath)) File.Delete(doc.FilePath);
            db.KnowledgeDocuments.Remove(doc);
            await db.SaveChangesAsync();
            logger.LogInformation("[RAG] Deleted document {Id} — {File}", documentId, doc.FileName);
        }
    }
}
