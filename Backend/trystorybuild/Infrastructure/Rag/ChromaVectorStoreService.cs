using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Rag
{
    public class ChromaVectorStoreService(
        HttpClient httpClient,
        IOptions<RagSettings> settings,
        ILogger<ChromaVectorStoreService> logger) : IVectorStoreService
    {
        private readonly RagSettings _cfg = settings.Value;
        private string? _collectionId;

        public async Task EnsureCollectionAsync()
        {
            var body = new { name = _cfg.CollectionName };
            var response = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/collections", body);
            if (response.StatusCode != System.Net.HttpStatusCode.Conflict)
                logger.LogInformation("[Chroma] Collection '{Name}' ready.", _cfg.CollectionName);
            _collectionId = await GetCollectionIdAsync();
        }

        public async Task DeleteBySourceAsync(string sourceFile)
        {
            var id = await GetCollectionIdAsync();
            var getResponse = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/collections/{id}/get",
                new { where = new { source_file = new { __eq = sourceFile } } });
            if (!getResponse.IsSuccessStatusCode) return;

            var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>();
            var ids  = json!.RootElement
                .GetProperty("ids").EnumerateArray()
                .Select(x => x.GetString()!).ToList();
            if (ids.Count == 0) return;

            await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/collections/{id}/delete",
                new { ids });
            logger.LogInformation("[Chroma] Deleted {Count} chunks for {Source}", ids.Count, sourceFile);
        }

        public async Task AddChunksAsync(
            List<string> ids, List<float[]> embeddings,
            List<string> texts, List<Dictionary<string, string>> metadatas)
        {
            var id   = await GetCollectionIdAsync();
            var body = new { ids, embeddings, documents = texts, metadatas };
            var response = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/collections/{id}/add", body);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("[Chroma] Stored {Count} chunks.", ids.Count);
        }

        public async Task<List<RagSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5)
        {
            var id      = await GetCollectionIdAsync();
            var request = new
            {
                query_embeddings = new[] { queryEmbedding },
                n_results        = topK,
                include          = new[] { "documents", "metadatas", "distances" }
            };
            var response = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/collections/{id}/query", request);
            response.EnsureSuccessStatusCode();

            var json        = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var results     = new List<RagSearchResult>();
            var docs        = json!.RootElement.GetProperty("documents")[0];
            var metas       = json!.RootElement.GetProperty("metadatas")[0];
            var distances   = json!.RootElement.GetProperty("distances")[0];
            var docArr      = docs.EnumerateArray().ToList();
            var metaArr     = metas.EnumerateArray().ToList();
            var distanceArr = distances.EnumerateArray().ToList();

            for (int i = 0; i < docArr.Count; i++)
            {
                var meta       = metaArr[i];
                var sourceFile = meta.TryGetProperty("source_file", out var sf) ? sf.GetString() ?? "" : "";
                var pageNum    = meta.TryGetProperty("page_number", out var pn)  ? (int?)pn.GetInt32() : null;
                results.Add(new RagSearchResult(
                    docArr[i].GetString() ?? "",
                    sourceFile, pageNum,
                    distanceArr[i].GetDouble()));
            }

            return results;
        }

        private async Task<string> GetCollectionIdAsync()
        {
            if (_collectionId is not null) return _collectionId;
            var response = await httpClient.GetAsync($"{_cfg.ChromaEndpoint}/api/v2/collections");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            foreach (var col in json!.RootElement.EnumerateArray())
            {
                if (col.GetProperty("name").GetString() == _cfg.CollectionName)
                {
                    _collectionId = col.GetProperty("id").GetString()!;
                    return _collectionId;
                }
            }
            throw new InvalidOperationException(
                $"Chroma collection '{_cfg.CollectionName}' not found. Call EnsureCollectionAsync first.");
        }
    }
}
