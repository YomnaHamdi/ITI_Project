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

        // ── Base path helpers (ChromaDB 0.6+ multi-tenant API) ─────────────────
        private string ColBase =>
            $"{_cfg.ChromaEndpoint}/api/v2/tenants/{_cfg.ChromaTenant}/databases/{_cfg.ChromaDatabase}/collections";

        private string ColPath(string collectionId) => $"{ColBase}/{collectionId}";

        public async Task EnsureCollectionAsync()
        {
            // Ensure tenant exists
            await EnsureTenantAsync();
            // Ensure database exists
            await EnsureDatabaseAsync();

            // Create collection (ignore 409 Conflict = already exists)
            var body     = new { name = _cfg.CollectionName };
            var response = await httpClient.PostAsJsonAsync(ColBase, body);
            if (response.StatusCode != System.Net.HttpStatusCode.Conflict
             && response.StatusCode != System.Net.HttpStatusCode.OK
             && response.StatusCode != System.Net.HttpStatusCode.Created
             && (int)response.StatusCode != 200 && (int)response.StatusCode != 201 && (int)response.StatusCode != 409)
            {
                var body2 = await response.Content.ReadAsStringAsync();
                logger.LogWarning("[Chroma] EnsureCollection status {S}: {B}", response.StatusCode, body2);
            }
            else
            {
                logger.LogInformation("[Chroma] Collection '{Name}' ready.", _cfg.CollectionName);
            }

            _collectionId = await GetCollectionIdAsync();
        }

        public async Task DeleteBySourceAsync(string sourceFile)
        {
            var id = await GetCollectionIdAsync();
            if (id is null) return;

            var getResponse = await httpClient.PostAsJsonAsync(
                $"{ColPath(id)}/get",
                new { where = new { source_file = new { __eq = sourceFile } } });
            if (!getResponse.IsSuccessStatusCode) return;

            var json = await getResponse.Content.ReadFromJsonAsync<JsonDocument>();
            var ids  = json!.RootElement
                .GetProperty("ids").EnumerateArray()
                .Select(x => x.GetString()!).ToList();
            if (ids.Count == 0) return;

            await httpClient.PostAsJsonAsync($"{ColPath(id)}/delete", new { ids });
            logger.LogInformation("[Chroma] Deleted {Count} chunks for {Source}", ids.Count, sourceFile);
        }

        public async Task AddChunksAsync(
            List<string> ids, List<float[]> embeddings,
            List<string> texts, List<Dictionary<string, string>> metadatas)
        {
            var id       = await GetCollectionIdAsync();
            var body     = new { ids, embeddings, documents = texts, metadatas };
            var response = await httpClient.PostAsJsonAsync($"{ColPath(id!)}/add", body);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                logger.LogError("[Chroma] AddChunks failed {S}: {E}", response.StatusCode, err);
                response.EnsureSuccessStatusCode();
            }
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
            var response = await httpClient.PostAsJsonAsync($"{ColPath(id!)}/query", request);
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
                var pageNum    = meta.TryGetProperty("page_number",  out var pn) ? (int?)pn.GetInt32() : null;
                results.Add(new RagSearchResult(
                    docArr[i].GetString() ?? "",
                    sourceFile, pageNum,
                    distanceArr[i].GetDouble()));
            }

            return results;
        }

        // ── Private helpers ────────────────────────────────────────────────────
        private async Task EnsureTenantAsync()
        {
            var url      = $"{_cfg.ChromaEndpoint}/api/v2/tenants/{_cfg.ChromaTenant}";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode) return;

            // 404 → create it
            var create = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/tenants",
                new { name = _cfg.ChromaTenant });
            if (!create.IsSuccessStatusCode && create.StatusCode != System.Net.HttpStatusCode.Conflict)
                logger.LogWarning("[Chroma] Could not create tenant '{T}': {S}", _cfg.ChromaTenant, create.StatusCode);
        }

        private async Task EnsureDatabaseAsync()
        {
            var url      = $"{_cfg.ChromaEndpoint}/api/v2/tenants/{_cfg.ChromaTenant}/databases/{_cfg.ChromaDatabase}";
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode) return;

            var create = await httpClient.PostAsJsonAsync(
                $"{_cfg.ChromaEndpoint}/api/v2/tenants/{_cfg.ChromaTenant}/databases",
                new { name = _cfg.ChromaDatabase });
            if (!create.IsSuccessStatusCode && create.StatusCode != System.Net.HttpStatusCode.Conflict)
                logger.LogWarning("[Chroma] Could not create database '{D}': {S}", _cfg.ChromaDatabase, create.StatusCode);
        }

        private async Task<string?> GetCollectionIdAsync()
        {
            if (_collectionId is not null) return _collectionId;

            var response = await httpClient.GetAsync(ColBase);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("[Chroma] GetCollections failed: {S}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
            foreach (var col in json!.RootElement.EnumerateArray())
            {
                if (col.GetProperty("name").GetString() == _cfg.CollectionName)
                {
                    _collectionId = col.GetProperty("id").GetString()!;
                    return _collectionId;
                }
            }

            logger.LogWarning("[Chroma] Collection '{Name}' not found after create.", _cfg.CollectionName);
            return null;
        }
    }
}
