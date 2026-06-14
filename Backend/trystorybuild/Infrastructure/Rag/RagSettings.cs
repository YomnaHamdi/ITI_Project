namespace Infrastructure.Rag
{
    public class RagSettings
    {
        public string OllamaEndpoint     { get; set; } = "http://localhost:11434";
        public string EmbeddingModel     { get; set; } = "nomic-embed-text";
        public string VisionModel        { get; set; } = "minicpm-v";
        public string ChromaEndpoint     { get; set; } = "http://localhost:8001";
        public string ChromaApiKey       { get; set; } = string.Empty;
        public string CollectionName     { get; set; } = "arabic_lessons";
        public string ChromaTenant       { get; set; } = "default_tenant";
        public string ChromaDatabase     { get; set; } = "default_database";
        public string DocumentsDirectory { get; set; } = "Uploads/Rag";
        public int    ChunkSize          { get; set; } = 400;
        public int    ChunkOverlap       { get; set; } = 60;
    }
}
