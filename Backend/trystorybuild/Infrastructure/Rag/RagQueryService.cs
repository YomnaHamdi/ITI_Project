using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Rag
{
    public class RagQueryService(
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStore,
        ILogger<RagQueryService> logger) : IRagQueryService
    {
        public async Task<string> GetContextAsync(string query, int topK = 5)
        {
            var results = await SearchAsync(query, topK);
            if (results.Count == 0)
            {
                logger.LogWarning("[RAG-Query] No results for: {Query}", query);
                return string.Empty;
            }
            return string.Join("\n\n---\n\n",
                results.Select((r, i) =>
                    $"[مصدر {i + 1}: {r.SourceFile}, صفحة {r.PageNumber}]\n{r.Text}"));
        }

        public async Task<List<RagSearchResult>> SearchAsync(string query, int topK = 5)
        {
            await vectorStore.EnsureCollectionAsync();
            var queryEmbedding = await embeddingService.GetEmbeddingAsync(query);
            if (queryEmbedding.Length == 0) return new();
            return await vectorStore.SearchAsync(queryEmbedding, topK);
        }
    }
}
